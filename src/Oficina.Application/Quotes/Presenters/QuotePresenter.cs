using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Quotes.Entities;

namespace Oficina.Application.Quotes.Presenters;

public sealed class QuotePresenter(IQuoteQueries queries, QuotePricing pricing)
{
    public async Task<QuoteDto> DtoAsync(Estimate estimate, CancellationToken cancellationToken)
    {
        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var totals = estimate.PriceOf(estimate.Lines, await pricing.TermsAsync(context?.Facts, cancellationToken));
        return new QuoteDto(
            estimate.Id,
            estimate.Number,
            estimate.ServiceOrderId,
            context?.ServiceOrderNumber ?? 0,
            estimate.Version,
            estimate.Status,
            new QuoteCustomerDto(context?.CustomerId ?? Guid.Empty, context?.CustomerName ?? string.Empty, MaskPhone(context?.Phone), MaskEmail(context?.Email),
                context is { ConsentWhatsApp: true, Phone: not null }, context is { ConsentEmail: true, Email: not null }, context?.Facts.Loyal ?? false, context?.Facts.FirstService ?? false, context?.Facts.Fleet ?? false),
            new QuoteVehicleDto(context?.LicensePlate ?? string.Empty, context?.Vehicle ?? string.Empty, context?.Mileage ?? 0),
            context?.Diagnosis,
            estimate.Lines.Select(Line).ToList(),
            estimate.DiscountMode,
            estimate.ManualDiscount is { } manual ? new ManualDiscountDto(manual.Type, manual.Value, manual.Reason) : null,
            new PaymentPlanDto(estimate.PaymentMethods.ToList(), estimate.MaxInstallments),
            estimate.Approval is { } approval ? Approval(approval) : null,
            estimate.Delivery is { } delivery ? Delivery(delivery) : null,
            estimate.RejectionReason,
            totals.Services,
            totals.Parts,
            totals.Discount,
            totals.Total,
            estimate.UpdatedAt ?? estimate.CreatedAt);
    }

    public static string? MaskPhone(string? phone) =>
        phone is { Length: >= 10 } ? $"({phone[..2]}) {phone[2]}••••-{phone[^4..]}" : null;

    public static string? MaskEmail(string? email) =>
        email?.Split('@') is [var name, var domain] && name.Length > 0 ? $"{name[0]}••••@{domain}" : null;

    public static QuoteLineDto Line(EstimateLine line) => new(line.Id, line.Kind, line.ItemId, line.Name, line.UnitPrice, line.Quantity, line.Source, line.Fractional);

    public static DiscountApprovalDto Approval(DiscountApproval approval) =>
        new(approval.Id, approval.Status, new DiscountDto(approval.Type, approval.Value), approval.Reason, approval.RequestedBy, approval.RequestedAt, approval.DecidedBy);

    public static QuoteDeliveryDto Delivery(EstimateDelivery delivery) =>
        new(delivery.Id, delivery.Channel, delivery.Status, delivery.Link, delivery.ExpiresAt, delivery.SentAt, delivery.OpenedAt, delivery.FailureReason, delivery.Attempts);
}
