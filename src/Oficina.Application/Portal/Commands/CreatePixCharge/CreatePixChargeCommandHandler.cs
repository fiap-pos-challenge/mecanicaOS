using MediatR;
using Oficina.Application.Abstractions.Payments;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Portal.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Portal.Repositories;

namespace Oficina.Application.Portal.Commands.CreatePixCharge;

public sealed class CreatePixChargeCommandHandler(
    PortalAccess access,
    IPortalRepository portal,
    IQuoteQueries quoteQueries,
    QuotePricing pricing,
    SettingsStore settings,
    IPixCodes pix,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreatePixChargeCommand, Result<PixChargeDto>>
{
    public const string DefaultCity = "SAO PAULO";
    public const int TxIdLength = 25;

    public async Task<Result<PixChargeDto>> Handle(CreatePixChargeCommand command, CancellationToken cancellationToken)
    {
        var found = await access.SessionAsync(command.Session, cancellationToken);
        if (found.Error is { } sessionError)
        {
            return sessionError;
        }

        var estimate = found.Value;
        var context = await quoteQueries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var due = estimate.AmountToCharge(await portal.PaidAtAsync(estimate.ServiceOrderId, cancellationToken) is not null, await pricing.TermsAsync(context?.Facts, cancellationToken));
        if (due.Error is { } dueError)
        {
            return dueError;
        }

        var payment = await pricing.PaymentsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(payment.PixKey))
        {
            return PortalErrors.PixUnavailable;
        }

        var now = timeProvider.GetUtcNow();
        var workshop = await settings.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(now), cancellationToken);
        var txId = Guid.CreateVersion7().ToString("N")[..TxIdLength].ToUpperInvariant();
        var charge = PortalCharge.Open(estimate.Id, estimate.ServiceOrderId, PortalCharge.PixMethod, due.Value, txId, now + PortalCharge.PixLifetime);
        portal.Add(charge);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        var payload = pix.Payload(payment.PixKey, workshop.Name, string.IsNullOrWhiteSpace(workshop.Address.City) ? DefaultCity : workshop.Address.City, due.Value, txId);
        return new PixChargeDto(charge.Id, payload, pix.QrCodeDataUrl(payload), charge.ExpiresAt);
    }
}
