using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Oficina.Application.Abstractions.Links;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Payments;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Portal.Documents;
using Oficina.Application.Portal.Dtos;
using Oficina.Application.Quotes;
using Oficina.Application.Quotes.Queries;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Documents.Entities;
using Oficina.Domain.Documents.Repositories;
using Oficina.Domain.Portal.Entities;
using Oficina.Domain.Portal.Errors;
using Oficina.Domain.Portal.Repositories;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.Portal;

public sealed partial class PortalService(
    IEstimateRepository estimates,
    IServiceOrderRepository serviceOrders,
    IPortalRepository portal,
    IDocumentRepository documents,
    IQuoteQueries quoteQueries,
    QuoteWorkflow workflow,
    SettingsStore settings,
    IPixCodes pix,
    IPublicLinks links,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<PortalService> logger,
    Notifications.INotificationPublisher notifications,
    Abstractions.Realtime.IRealtimePublisher realtime,
    Automations.AutomationService automations)
{
    public const int MaxAttempts = 3;
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<Result<PortalAccessDto>> AccessAsync(string token, CancellationToken cancellationToken)
    {
        var estimate = await LinkAsync(token, cancellationToken);
        if (estimate.Error is { } error)
        {
            return error;
        }

        return new PortalAccessDto(MaxAttempts - estimate.Value.PortalFailures);
    }

    public async Task<Result<PortalConfirmDto>> ConfirmAsync(string token, string lastDigits, CancellationToken cancellationToken)
    {
        var found = await LinkAsync(token, cancellationToken);
        if (found.Error is { } error)
        {
            return error;
        }

        var estimate = found.Value;
        var context = await quoteQueries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (context?.Phone is not { Length: >= 3 } phone || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(phone[^3..]), Encoding.ASCII.GetBytes((lastDigits ?? string.Empty).Trim())))
        {
            estimate.RegisterPortalFailure();
            await unitOfWork.CommitAsync(cancellationToken);
            return estimate.PortalFailures >= MaxAttempts ? PortalErrors.Locked : new PortalConfirmDto("WrongDigits", null, MaxAttempts - estimate.PortalFailures, null);
        }

        estimate.ResetPortalFailures();
        estimate.MarkOpened(now);
        var sessionToken = InvitationToken.Generate();
        var session = PortalSession.Start(InvitationToken.Hash(sessionToken), estimate.Id, now);
        portal.Add(session);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return new PortalConfirmDto("Confirmed", sessionToken, MaxAttempts, session.ExpiresAt);
    }

    public async Task<Result<PortalOrderDto>> OrderAsync(string? session, CancellationToken cancellationToken)
    {
        var estimate = await SessionAsync(session, cancellationToken);
        return estimate.Error is { } error ? error : await ViewAsync(estimate.Value, cancellationToken);
    }

    public async Task<Result<PortalOrderDto>> ApproveAsync(string? session, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken)
    {
        var found = await SessionAsync(session, cancellationToken);
        if (found.Error is { } error)
        {
            return error;
        }

        var estimate = found.Value;
        if (estimate.Status != EstimateStatus.Pending)
        {
            return PortalErrors.AlreadyAnswered;
        }

        var accepted = itemIds.Where(id => estimate.Lines.Exists(line => line.Id == id)).Distinct().ToList();
        if (accepted.Count == 0)
        {
            return PortalErrors.NoItems;
        }

        var approved = await workflow.ApproveAsync(estimate.Id, accepted, cancellationToken);
        return approved.Error is { } approveError ? approveError : await ViewAsync(estimate, cancellationToken);
    }

    public async Task<Result<PortalOrderDto>> RejectAsync(string? session, string reason, CancellationToken cancellationToken)
    {
        var found = await SessionAsync(session, cancellationToken);
        if (found.Error is { } error)
        {
            return error;
        }

        if (found.Value.Status != EstimateStatus.Pending)
        {
            return PortalErrors.AlreadyAnswered;
        }

        var rejected = await workflow.RejectAsync(found.Value.Id, reason, cancellationToken);
        return rejected.Error is { } rejectError ? rejectError : await ViewAsync(found.Value, cancellationToken);
    }

    public async Task<Result<PixChargeDto>> PixAsync(string? session, CancellationToken cancellationToken)
    {
        var found = await SessionAsync(session, cancellationToken);
        if (found.Error is { } error)
        {
            return error;
        }

        var estimate = found.Value;
        var due = await AmountDueAsync(estimate, cancellationToken);
        if (due <= 0)
        {
            return PortalErrors.NothingToPay;
        }

        var payment = await settings.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);
        if (string.IsNullOrWhiteSpace(payment.PixKey))
        {
            return PortalErrors.PixUnavailable;
        }

        var workshop = await settings.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        var txId = Guid.CreateVersion7().ToString("N")[..25].ToUpperInvariant();
        var charge = PortalCharge.Open(estimate.Id, estimate.ServiceOrderId, "Pix", due, txId, timeProvider.GetUtcNow() + PortalCharge.PixLifetime);
        portal.Add(charge);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        var payload = pix.Payload(payment.PixKey, workshop.Name, string.IsNullOrWhiteSpace(workshop.Address.City) ? "SAO PAULO" : workshop.Address.City, due, txId);
        return new PixChargeDto(charge.Id, payload, pix.QrCodeDataUrl(payload), charge.ExpiresAt);
    }

    public async Task<Result<ChargeStatusDto>> ChargeAsync(string? session, Guid paymentId, CancellationToken cancellationToken)
    {
        var found = await SessionAsync(session, cancellationToken);
        if (found.Error is { } error)
        {
            return error;
        }

        if (await portal.GetChargeAsync(paymentId, cancellationToken) is not { } charge || charge.EstimateId != found.Value.Id)
        {
            return PortalErrors.PaymentNotFound;
        }

        var status = charge.Refresh(timeProvider.GetUtcNow());
        await unitOfWork.CommitAsync(cancellationToken);
        return new ChargeStatusDto(status);
    }

    public async Task<Result<CardRedirectDto>> CardAsync(string? session, CancellationToken cancellationToken)
    {
        var found = await SessionAsync(session, cancellationToken);
        if (found.Error is { } error)
        {
            return error;
        }

        return await AmountDueAsync(found.Value, cancellationToken) <= 0 ? PortalErrors.NothingToPay : PortalErrors.CardUnavailable;
    }

    public async Task<Result> ConfirmPixAsync(string txId, string signature, string secret, CancellationToken cancellationToken)
    {
        var expected = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(txId)));
        if (string.IsNullOrWhiteSpace(secret) || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant())))
        {
            return PortalErrors.InvalidWebhook;
        }

        if (await portal.GetChargeByTxIdAsync(txId, cancellationToken) is not { } charge)
        {
            return PortalErrors.PaymentNotFound;
        }

        if (charge.MarkPaid(timeProvider.GetUtcNow()) && await serviceOrders.GetByIdAsync(charge.ServiceOrderId, cancellationToken) is { } order && await quoteQueries.ContextAsync(order.Id, cancellationToken) is { } context)
        {
            var issuer = new DocumentIssuer(documents, settings, timeProvider);
            await issuer.IssueAsync(DocumentIssuer.ReceiptKind, order, context.CustomerName, $"{context.LicensePlate} · {context.Vehicle}", await estimates.GetByIdAsync(charge.EstimateId, cancellationToken), workflow, charge.PaidAt, cancellationToken);
            await notifications.PublishAsync("PaymentConfirmed", $"Pagamento da OS {order.Number.Value} confirmado", "Pix recebido pelo portal do cliente.", "/service-orders", false, null, cancellationToken);
            await automations.FireAsync("PaymentReceived", order.Id, $"payment:{charge.Id:N}", cancellationToken);
            await realtime.PublishAsync(new { type = "paymentConfirmed", serviceOrderId = order.Id, number = order.Number.Value, amount = (long)Common.Numbers.Rounding.HalfUp(charge.Amount * 100, 0), occurredAt = charge.PaidAt }, cancellationToken);
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<Result<DocumentDto>> DocumentAsync(string code, Guid? serviceOrderId, CancellationToken cancellationToken)
    {
        if (await documents.GetByCodeAsync(code, cancellationToken) is not { } document || (serviceOrderId is { } orderId && document.ServiceOrderId != orderId))
        {
            return PortalErrors.DocumentNotFound;
        }

        return Document(document);
    }

    public async Task<Result<DocumentDownloadDto>> PdfAsync(string code, Guid? serviceOrderId, CancellationToken cancellationToken)
    {
        var found = await DocumentAsync(code, serviceOrderId, cancellationToken);
        if (found.Error is { } error)
        {
            return error;
        }

        var document = found.Value;
        var title = document.Kind switch { "Quote" => "Orçamento", "Receipt" => "Recibo", _ => "Ordem de serviço" };
        var lines = new List<string>
        {
            document.ShopName,
            $"{title} nº {document.Number}{(document.Version is { } version ? $" · versão {version}" : string.Empty)}",
            $"Emitido em {document.IssuedAt.ToString("dd/MM/yyyy HH:mm", Brazil)}",
            $"Cliente: {document.CustomerName}",
            $"Veículo: {document.Vehicle}",
            string.Empty,
        };
        lines.AddRange(document.Lines.Select(line => $"{line.Name} · {line.Quantity.ToString("0.###", Brazil)} x {line.UnitPrice.ToString("C", Brazil)} = {line.Total.ToString("C", Brazil)}"));
        lines.Add(string.Empty);
        lines.Add($"Serviços: {document.ServicesTotal.ToString("C", Brazil)}   Peças: {document.PartsTotal.ToString("C", Brazil)}   Desconto: {document.DiscountTotal.ToString("C", Brazil)}");
        lines.Add($"Total: {document.Total.ToString("C", Brazil)}");
        if (document.PaidAt is { } paid)
        {
            lines.Add($"Pago em {paid.ToString("dd/MM/yyyy HH:mm", Brazil)}");
        }

        lines.Add(string.Empty);
        lines.Add($"Código {document.Code} · confira em {links.DocumentVerification(document.Code)}");
        return new DocumentDownloadDto($"data:application/pdf;base64,{Convert.ToBase64String(PdfWriter.Write(lines))}", $"{document.Code}.pdf");
    }

    public async Task<Result<DocumentSendDto>> SendDocumentAsync(string code, string channel, CancellationToken cancellationToken)
    {
        if (channel is not ("WhatsApp" or "Email"))
        {
            return PortalErrors.InvalidChannel;
        }

        if (await documents.GetByCodeAsync(code, cancellationToken) is not { } document)
        {
            return PortalErrors.DocumentNotFound;
        }

        var context = await quoteQueries.ContextAsync(document.ServiceOrderId, cancellationToken);
        if (channel == "Email" && context is { ConsentEmail: true, Email: { } email })
        {
            var sent = await emailSender.SendAsync(new EmailMessage(email, $"{document.Snapshot.ShopName} · documento {document.Code}",
                $"<p>Olá, {System.Net.WebUtility.HtmlEncode(context.CustomerName.Split(' ')[0])}! Segue o documento {document.Code}.</p><p><a href=\"{links.DocumentVerification(document.Code)}\">Conferir o documento</a></p>"), cancellationToken);
            return new DocumentSendDto(sent ? "Sent" : "Failed");
        }

        LogDocumentQueued(logger, channel);
        return new DocumentSendDto("Queued");
    }

    public async Task<Result<DocumentVerificationDto>> VerifyAsync(string code, CancellationToken cancellationToken) =>
        await documents.GetByCodeAsync(code, cancellationToken) is { } document
            ? new DocumentVerificationDto(true, document.Kind, document.Snapshot.Number, document.IssuedAt, document.Snapshot.Total, document.Snapshot.ShopName)
            : new DocumentVerificationDto(false, null, null, null, null, null);

    public async Task<Guid?> SessionOrderAsync(string? session, CancellationToken cancellationToken)
    {
        var found = await SessionAsync(session, cancellationToken);
        return found.IsSuccess ? found.Value.ServiceOrderId : null;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Document queued for {Channel}")]
    private static partial void LogDocumentQueued(ILogger logger, string channel);

    private static DocumentDto Document(IssuedDocument document)
    {
        var snapshot = document.Snapshot;
        return new DocumentDto(document.Kind, document.Code, snapshot.Number, snapshot.Version, document.IssuedAt, snapshot.ShopName, snapshot.CustomerName, snapshot.Vehicle,
            snapshot.Lines.Select(line => new DocumentLineDto(line.Name, line.Quantity, line.UnitPrice, line.Total)).ToList(),
            snapshot.ServicesTotal, snapshot.PartsTotal, snapshot.DiscountTotal, snapshot.Total, snapshot.PaidAt, snapshot.PaymentMethods);
    }

    private async Task<Result<Estimate>> LinkAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || await estimates.GetByDeliveryTokenAsync(InvitationToken.Hash(token.Trim()), cancellationToken) is not { Delivery: { } delivery } estimate
            || delivery.ExpiresAt <= timeProvider.GetUtcNow())
        {
            return PortalErrors.LinkUnavailable;
        }

        return estimate.PortalFailures >= MaxAttempts ? PortalErrors.Locked : estimate;
    }

    private async Task<Result<Estimate>> SessionAsync(string? session, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session) || await portal.GetSessionAsync(InvitationToken.Hash(session.Trim()), cancellationToken) is not { } found
            || found.ExpiresAt <= timeProvider.GetUtcNow() || await estimates.GetByIdAsync(found.EstimateId, cancellationToken) is not { } estimate)
        {
            return PortalErrors.SessionExpired;
        }

        return await estimates.GetLatestByNumberAsync(estimate.Number, cancellationToken) ?? estimate;
    }

    private async Task<decimal> AmountDueAsync(Estimate estimate, CancellationToken cancellationToken)
    {
        if (estimate.Status != EstimateStatus.Approved || await portal.PaidAtAsync(estimate.ServiceOrderId, cancellationToken) is not null)
        {
            return 0;
        }

        var accepted = estimate.Lines.Where(line => estimate.AcceptedLineIds.Contains(line.Id)).ToList();
        return (await workflow.TotalsAsync(estimate, accepted, cancellationToken)).Total;
    }

    private async Task<PortalOrderDto> ViewAsync(Estimate estimate, CancellationToken cancellationToken)
    {
        var current = await estimates.GetLatestByNumberAsync(estimate.Number, cancellationToken) ?? estimate;
        var order = (await serviceOrders.GetByIdAsync(current.ServiceOrderId, cancellationToken))!;
        var context = (await quoteQueries.ContextAsync(order.Id, cancellationToken))!;
        var workshop = await settings.GetAsync(SettingsKeys.Workshop, SettingsDefaults.Workshop(timeProvider.GetUtcNow()), cancellationToken);
        var payment = await settings.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);
        var lines = current.Status == EstimateStatus.Approved ? current.Lines.Where(line => current.AcceptedLineIds.Contains(line.Id)).ToList() : current.Lines;
        var totals = await workflow.TotalsAsync(current, lines, cancellationToken);
        var paidAt = await portal.PaidAtAsync(order.Id, cancellationToken);
        var docs = await documents.ListByServiceOrderAsync(order.Id, cancellationToken);
        return new PortalOrderDto(
            order.Number.Value,
            workshop.Name,
            context.CustomerName.Split(' ')[0],
            context.Vehicle,
            context.LicensePlate,
            order.Status.ToString(),
            order.StatusHistory.OrderBy(entry => entry.ChangedAt).Select(entry => new PortalHistoryDto(entry.NewStatus.ToString(), entry.ChangedAt)).ToList(),
            order.Photos.OrderBy(photo => photo.TakenAt).Select(photo => new PortalPhotoDto(photo.Id, photo.DataUrl, photo.Caption)).ToList(),
            new PortalQuoteDto(current.Number, current.Version, current.Status,
                lines.Select(line => new PortalQuoteItemDto(line.Id, line.Name, line.Kind == "Part" ? "Part" : "Service", line.UnitPrice * line.Quantity)).ToList(), totals.Discount, totals.Total),
            current.Status == EstimateStatus.Approved ? new PortalPaymentDto(paidAt is null ? "Pending" : "Paid", paidAt is null ? totals.Total : 0, payment.Methods.Where(method => method is "Pix" or "Credit").ToList()) : null,
            docs.OrderBy(document => document.IssuedAt).Select(document => new PortalDocumentDto(document.Kind, document.Code, document.IssuedAt)).ToList());
    }
}
