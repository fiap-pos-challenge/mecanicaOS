using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Oficina.Application.Automations;
using Oficina.Application.Reports;
using Oficina.Domain.Automations;
using Oficina.Domain.Jobs.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Operations;

public sealed class AutomationQueries(OficinaDbContext context, IOptions<OutboxOptions> outbox, TimeProvider timeProvider) : IAutomationQueries
{
    private static readonly TimeSpan MaintenanceAfter = TimeSpan.FromDays(180);

    public async Task<AutomationContext?> ContextAsync(string trigger, Guid? subjectId, CancellationToken cancellationToken)
    {
        if (subjectId is not { } id)
        {
            return null;
        }

        if (trigger == "StockBelowMinimum")
        {
            return await context.Parts.AsNoTracking().SingleOrDefaultAsync(part => part.Id == id, cancellationToken) is { } part
                ? new AutomationContext(null, null, null, null, null, null, false, false, null, null, null, null, part.StockQuantity, part.Id, part.Name, null)
                : null;
        }

        if (trigger == "VehicleMaintenanceDue")
        {
            if (await context.Customers.AsNoTracking().SingleOrDefaultAsync(customer => customer.Id == id, cancellationToken) is not { } customer)
            {
                return null;
            }

            var vehicle = await context.Vehicles.AsNoTracking().Where(row => row.CustomerId == id).OrderByDescending(row => row.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            return new AutomationContext(null, null, customer.Name, vehicle is null ? null : $"{vehicle.Brand} {vehicle.Model}", customer.Email?.Value, customer.Phone.Value, customer.ConsentEmail, customer.ConsentWhatsApp, null, null, null, null, null, null, null, null);
        }

        if (await context.ServiceOrders.AsNoTracking().SingleOrDefaultAsync(order => order.Id == id, cancellationToken) is not { } serviceOrder)
        {
            return null;
        }

        var owner = await context.Customers.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(customer => customer.Id == serviceOrder.CustomerId, cancellationToken);
        var car = await context.Vehicles.AsNoTracking().IgnoreQueryFilters().SingleOrDefaultAsync(row => row.Id == serviceOrder.VehicleId, cancellationToken);
        var estimate = await context.Estimates.AsNoTracking().Where(row => row.ServiceOrderId == id).OrderByDescending(row => row.Version).FirstOrDefaultAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        int? overdue = serviceOrder.Status == ServiceOrderStatus.Completed && serviceOrder.CompletedAt is { } completed ? (int)(now - completed).TotalDays : null;
        return new AutomationContext(serviceOrder.Id, serviceOrder.Number.Value, owner?.Name, car is null ? null : $"{car.Brand} {car.Model}", owner?.Email?.Value, owner?.Phone.Value,
            owner?.ConsentEmail ?? false, owner?.ConsentWhatsApp ?? false, serviceOrder.TotalAmount.Amount, serviceOrder.Status.ToString(), estimate?.Status, overdue, null, null, null, estimate?.Delivery?.Link);
    }

    public async Task<IReadOnlyList<ScanSubject>> ScanAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var day = now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var month = now.ToString("yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
        var low = await context.Parts.AsNoTracking().Where(part => part.IsActive && part.MinimumStock > 0 && part.StockQuantity < part.MinimumStock).Select(part => part.Id).ToListAsync(cancellationToken);
        var overdue = await context.ServiceOrders.AsNoTracking().Where(order => order.Status == ServiceOrderStatus.Completed && order.CompletedAt != null && order.CompletedAt < now.AddDays(-1)).Select(order => order.Id).ToListAsync(cancellationToken);
        var visits = await context.ServiceOrders.AsNoTracking().GroupBy(order => order.CustomerId).Select(group => new { group.Key, Last = group.Max(order => order.CreatedAt) }).ToListAsync(cancellationToken);
        return low.Select(id => new ScanSubject("StockBelowMinimum", id, $"stock:{id:N}:{day}"))
            .Concat(overdue.Select(id => new ScanSubject("PaymentOverdue", id, $"overdue:{id:N}:{day}")))
            .Concat(visits.Where(visit => now - visit.Last >= MaintenanceAfter).Select(visit => new ScanSubject("VehicleMaintenanceDue", visit.Key, $"maintenance:{visit.Key:N}:{month}")))
            .ToList();
    }

    public async Task<IReadOnlyList<RuleStats>> StatsAsync(DateTimeOffset since, CancellationToken cancellationToken) =>
        (await context.AutomationExecutions.AsNoTracking().Where(execution => execution.Status == ExecutionStatus.Done)
            .GroupBy(execution => execution.RuleId)
            .Select(group => new { group.Key, Runs = group.Count(execution => execution.FinishedAt >= since), Last = group.Max(execution => execution.FinishedAt) })
            .ToListAsync(cancellationToken))
        .Select(row => new RuleStats(row.Key, row.Runs, row.Last)).ToList();

    public async Task<IReadOnlyList<QueueHealthDto>> QueuesAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var max = outbox.Value.MaxAttempts;
        var messages = context.OutboxMessages.AsNoTracking();
        var jobs = context.BackgroundJobs.AsNoTracking().Where(job => job.CreatedAt >= since);
        var executions = context.AutomationExecutions.AsNoTracking();
        var now = timeProvider.GetUtcNow();
        return
        [
            new("notifications", await messages.CountAsync(message => message.ProcessedAt >= since, cancellationToken), await messages.CountAsync(message => message.ProcessedAt == null && message.Attempts >= max, cancellationToken), await messages.CountAsync(message => message.ProcessedAt == null && message.Attempts < max, cancellationToken)),
            new("documents", await context.IssuedDocuments.CountAsync(document => document.IssuedAt >= since, cancellationToken), 0, 0),
            new("ai", 0, 0, 0),
            new("payments", await context.PortalCharges.CountAsync(charge => charge.PaidAt >= since, cancellationToken), 0, await context.PortalCharges.CountAsync(charge => charge.Status == "Pending" && charge.ExpiresAt > now, cancellationToken)),
            new("imports", await jobs.CountAsync(job => job.Kind == BackgroundJob.CustomerImport && job.Status == "Completed", cancellationToken), await jobs.CountAsync(job => job.Kind == BackgroundJob.CustomerImport && job.Status == "Failed", cancellationToken), 0),
            new("reports", await jobs.CountAsync(job => job.Kind == ReportService.ExportKind, cancellationToken), 0, 0),
            new("automations", await executions.CountAsync(execution => execution.Status == ExecutionStatus.Done && execution.FinishedAt >= since, cancellationToken), await executions.CountAsync(execution => execution.Status == ExecutionStatus.Failed, cancellationToken), await executions.CountAsync(execution => execution.Status == ExecutionStatus.Pending, cancellationToken)),
        ];
    }

    public async Task<IReadOnlyList<QueueFailureDto>> FailuresAsync(CancellationToken cancellationToken)
    {
        var max = outbox.Value.MaxAttempts;
        var failedExecutions = await context.AutomationExecutions.AsNoTracking().Where(execution => execution.Status == ExecutionStatus.Failed).OrderByDescending(execution => execution.FinishedAt).Take(50).ToListAsync(cancellationToken);
        var failedMessages = await context.OutboxMessages.AsNoTracking().Where(message => message.ProcessedAt == null && message.Attempts >= max).OrderByDescending(message => message.OccurredAt).Take(50).ToListAsync(cancellationToken);
        return failedExecutions.Select(execution => new QueueFailureDto(execution.Id, "automations", execution.Summary, execution.Error ?? string.Empty, execution.Attempts, execution.FinishedAt ?? execution.CreatedAt))
            .Concat(failedMessages.Select(message => new QueueFailureDto(message.Id, "notifications", message.Type, message.Error ?? string.Empty, message.Attempts, message.OccurredAt)))
            .OrderByDescending(failure => failure.FailedAt).ToList();
    }

    public async Task<bool> RetryOutboxAsync(Guid id, CancellationToken cancellationToken)
    {
        var max = outbox.Value.MaxAttempts;
        if (await context.OutboxMessages.SingleOrDefaultAsync(message => message.Id == id && message.ProcessedAt == null && message.Attempts >= max, cancellationToken) is not { } message)
        {
            return false;
        }

        message.ResetAttempts();
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
