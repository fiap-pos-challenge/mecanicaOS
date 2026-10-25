using Oficina.Application.Portal.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Application.ServiceOrders;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Documents.Repositories;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Portal.Repositories;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.Portal;

public sealed class PortalOrderView(
    IEstimateRepository estimates,
    IServiceOrderRepository serviceOrders,
    IServiceOrderPhotoRepository photos,
    IPortalRepository portal,
    IDocumentRepository documents,
    IQuoteQueries quoteQueries,
    QuotePricing pricing,
    SettingsStore settings,
    TimeProvider timeProvider)
{
    public static readonly IReadOnlyList<string> PortalMethods = [PortalCharge.PixMethod, "Credit"];

    public async Task<Result<PortalOrderDto>> ViewAsync(Estimate estimate, CancellationToken cancellationToken)
    {
        var current = await estimates.GetLatestByNumberAsync(estimate.Number, cancellationToken) ?? estimate;
        if (await serviceOrders.GetByIdAsync(current.ServiceOrderId, cancellationToken) is not { } order || await quoteQueries.ContextAsync(order.Id, cancellationToken) is not { } context)
        {
            return ServiceOrderErrors.NotFound;
        }

        var workshop = await settings.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        var payment = await pricing.PaymentsAsync(cancellationToken);
        var totals = current.Price(await pricing.TermsAsync(context.Facts, cancellationToken));
        var paidAt = await portal.PaidAtAsync(order.Id, cancellationToken);
        var docs = await documents.ListByServiceOrderAsync(order.Id, cancellationToken);
        var pictures = await photos.ListSummariesAsync([order.Id], cancellationToken);
        return new PortalOrderDto(
            order.Number.Value,
            workshop.Name,
            context.CustomerName.Split(' ')[0],
            context.Vehicle,
            context.LicensePlate,
            order.Status.ToString(),
            order.StatusHistory.OrderBy(entry => entry.ChangedAt).Select(entry => new PortalHistoryDto(entry.NewStatus.ToString(), entry.ChangedAt)).ToList(),
            pictures.Select(photo => new PortalPhotoDto(photo.Id, ServiceOrderPhotoPaths.Portal(photo.Id), photo.Caption)).ToList(),
            new PortalQuoteDto(current.Number, current.Version, current.Status,
                current.EffectiveLines.Select(line => new PortalQuoteItemDto(line.Id, line.Name, line.Kind == EstimateLineKind.Part ? EstimateLineKind.Part : EstimateLineKind.Service, line.UnitPrice * line.Quantity)).ToList(), totals.Discount, totals.Total),
            current.Status == EstimateStatus.Approved ? new PortalPaymentDto(paidAt is null ? PortalCharge.Pending : PortalCharge.Paid, paidAt is null ? totals.Total : 0, payment.Methods.Where(PortalMethods.Contains).ToList()) : null,
            docs.OrderBy(document => document.IssuedAt).Select(document => new PortalDocumentDto(document.Kind, document.Code, document.IssuedAt)).ToList());
    }
}
