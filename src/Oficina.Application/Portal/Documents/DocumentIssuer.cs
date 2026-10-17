using Oficina.Application.Quotes;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Documents.Entities;
using Oficina.Domain.Documents.Repositories;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.ServiceOrders.Entities;

namespace Oficina.Application.Portal.Documents;

public sealed class DocumentIssuer(IDocumentRepository documents, SettingsStore settings, TimeProvider timeProvider)
{
    public const string ServiceOrderKind = "ServiceOrder";
    public const string QuoteKind = "Quote";
    public const string ReceiptKind = "Receipt";

    public async Task IssueAsync(string kind, ServiceOrder order, string customerName, string vehicle, Estimate? estimate, QuoteWorkflow? workflow, DateTimeOffset? paidAt, CancellationToken cancellationToken)
    {
        if (kind != QuoteKind && await documents.HasAsync(order.Id, kind, cancellationToken))
        {
            return;
        }

        var shop = (await settings.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken)).Name;
        var payment = await settings.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);
        IReadOnlyList<DocumentLine> lines;
        decimal services, parts, discount, total;
        if (estimate is not null && workflow is not null)
        {
            var accepted = estimate.Status == EstimateStatus.Approved ? estimate.Lines.Where(line => estimate.AcceptedLineIds.Contains(line.Id)).ToList() : estimate.Lines;
            var totals = await workflow.TotalsAsync(estimate, accepted, cancellationToken);
            lines = accepted.Select(line => new DocumentLine(line.Name, line.Quantity, line.UnitPrice, line.UnitPrice * line.Quantity)).ToList();
            (services, parts, discount, total) = (totals.Services, totals.Parts, totals.Discount, totals.Total);
        }
        else
        {
            lines = order.ServiceItems.Select(item => new DocumentLine(item.Description, item.Quantity, item.UnitPrice.Amount, item.Total.Amount))
                .Concat(order.PartItems.Select(item => new DocumentLine(item.Description, item.Quantity.Value, item.UnitPrice.Amount, item.Total.Amount)))
                .ToList();
            services = order.ServiceItems.Sum(item => item.Total.Amount);
            parts = order.PartItems.Sum(item => item.Total.Amount);
            total = order.TotalAmount.Amount;
            discount = Math.Max(0, services + parts - total);
        }

        var prefix = kind switch { QuoteKind => "ORC", ReceiptKind => "REC", _ => "OS" };
        var code = $"{prefix}-{order.Number.Value}-{Guid.CreateVersion7().ToString("N")[^6..].ToUpperInvariant()}";
        var number = kind == QuoteKind && estimate is not null ? estimate.Number : order.Number.Value;
        documents.Add(IssuedDocument.Issue(code, kind, order.Id, timeProvider.GetUtcNow(), new DocumentSnapshot(
            number, kind == QuoteKind ? estimate?.Version : null, shop, customerName, vehicle, lines, services, parts, discount, total, paidAt, payment.Methods)));
    }
}
