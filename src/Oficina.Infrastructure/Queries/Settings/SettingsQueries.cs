using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Dtos;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Privacy.Enums;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Settings;

public sealed class SettingsQueries(OficinaDbContext context) : ISettingsQueries
{
    public async Task<IReadOnlyList<SessionDto>> SessionsAsync(Guid? currentSessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rows = await (from session in context.UserSessions.AsNoTracking()
                          join user in context.Users.AsNoTracking() on session.UserId equals user.Id
                          where session.RevokedAt == null && session.ExpiresAt > now
                          orderby session.LastSeenAt descending
                          select new { session.Id, user.Name, session.Device, session.LastSeenAt })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new SessionDto(row.Id, row.Name, row.Device, row.LastSeenAt, row.Id == currentSessionId)).ToList();
    }

    public Task<int> ActiveUsersAsync(CancellationToken cancellationToken) => context.Users.CountAsync(user => user.IsActive, cancellationToken);

    public async Task<PagedResult<AuditEntryDto>> AuditAsync(AuditFilter filter, CancellationToken cancellationToken)
    {
        var since = filter.From is { } start ? new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : DateTimeOffset.MinValue;
        var until = filter.To is { } end ? new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : DateTimeOffset.MaxValue;
        var entries = new List<AuditEntryDto>();
        if (filter.Area is null or "discounts" or "payments" or "security")
        {
            var query = context.AuditEntries.AsNoTracking().Where(entry => entry.OccurredAt >= since && entry.OccurredAt < until);
            if (filter.Area is { } area)
            {
                query = query.Where(entry => entry.Area == area);
            }

            entries.AddRange((await query.ToListAsync(cancellationToken)).Select(entry => new AuditEntryDto(entry.Id, entry.OccurredAt, entry.ActorName, entry.Area, entry.Action, entry.Target, entry.Detail)));
        }

        if (filter.Area is null or "customers")
        {
            var reveals = await (from access in context.SensitiveDataAccesses.AsNoTracking()
                                 join user in context.Users.AsNoTracking() on access.UserId equals user.Id
                                 join customer in context.Customers.AsNoTracking().IgnoreQueryFilters() on access.CustomerId equals customer.Id
                                 where access.OccurredAt >= since && access.OccurredAt < until
                                 select new { access.Id, access.OccurredAt, user.Name, Customer = customer.Name, access.Field, access.Reason })
                .ToListAsync(cancellationToken);
            entries.AddRange(reveals.Select(row => new AuditEntryDto(row.Id, row.OccurredAt, row.Name, "customers", "Dado pessoal revelado", $"Cliente {Initials(row.Customer)}", $"{Field(row.Field)}, motivo: {row.Reason}")));
        }

        if (filter.Area is null or "stock")
        {
            var adjustments = await (from movement in context.StockMovements.AsNoTracking()
                                     join part in context.Parts.AsNoTracking().IgnoreQueryFilters() on movement.PartId equals part.Id
                                     join user in context.Users.AsNoTracking() on movement.UserId equals user.Id into actors
                                     from actor in actors.DefaultIfEmpty()
                                     where movement.Type == StockMovementType.Adjustment && movement.CreatedAt >= since && movement.CreatedAt < until
                                     select new { movement.Id, movement.CreatedAt, Actor = actor == null ? "Sistema" : actor.Name, Part = part.Name, movement.Reason })
                .ToListAsync(cancellationToken);
            entries.AddRange(adjustments.Select(row => new AuditEntryDto(row.Id, row.CreatedAt, row.Actor, "stock", "Ajuste de estoque", row.Part, row.Reason)));
        }

        var filtered = entries
            .Where(entry => string.IsNullOrEmpty(filter.Actor) || entry.ActorName.Contains(filter.Actor, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.OccurredAt)
            .ToList();
        return new PagedResult<AuditEntryDto>(filtered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList(), filter.Page, filter.PageSize, filtered.Count);
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch { 0 => string.Empty, 1 => parts[0], _ => $"{parts[0]} {parts[^1][0]}." };
    }

    private static string Field(SensitiveField field) => field switch
    {
        SensitiveField.Phone => "Telefone",
        SensitiveField.Email => "E-mail",
        SensitiveField.Document => "Documento",
        _ => field.ToString(),
    };
}
