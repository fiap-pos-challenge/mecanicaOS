using System.Globalization;
using Oficina.Application.Abstractions.Links;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Portal.Documents;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Audit.Entities;
using Oficina.Domain.Audit.Repositories;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Documents.Repositories;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.Services;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.Domain.Team.Repositories;

namespace Oficina.Application.Quotes;

public sealed class QuoteWorkflow(
    IEstimateRepository estimates,
    IServiceOrderRepository serviceOrders,
    IServiceRepository services,
    IPartRepository parts,
    IPackageRepository packages,
    IUserRepository users,
    ITeamProfileRepository profiles,
    IQuoteQueries queries,
    SettingsStore settings,
    QuoteApprovalPolicy approvalPolicy,
    IPublicLinks links,
    IEmailSender emailSender,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IDocumentRepository documents,
    IAuditRepository audit,
    Notifications.INotificationPublisher notifications)
{
    public const int MaxMessageLength = 1000;
    private static readonly string[] Kinds = ["Package", "Service", "Part"];
    private static readonly string[] Channels = ["WhatsApp", "Email", "Link"];
    private static readonly string[] Methods = ["Pix", "Credit", "Debit", "Cash", "Boleto"];
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<Result<PagedResult<QuoteSummaryDto>>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken)
    {
        var request = new PageRequest(page, pageSize);
        var latest = await queries.LatestAsync(status, cancellationToken);
        var pageItems = latest.Skip(request.Skip).Take(request.PageSize).ToList();
        var contexts = await queries.ContextsAsync(pageItems.Select(estimate => estimate.ServiceOrderId).Distinct().ToList(), cancellationToken);
        var discounts = await DiscountsAsync(cancellationToken);
        var items = pageItems.Select(estimate =>
        {
            var context = contexts.GetValueOrDefault(estimate.ServiceOrderId);
            var totals = Totals(estimate, discounts, context?.Facts, 100);
            return new QuoteSummaryDto(estimate.Id, estimate.Number, estimate.Version, estimate.Status, context?.CustomerName ?? string.Empty, context?.LicensePlate ?? string.Empty,
                totals.Total, estimate.Delivery?.Status, estimate.Approval?.Status == "Pending", estimate.UpdatedAt ?? estimate.CreatedAt);
        }).ToList();
        return new PagedResult<QuoteSummaryDto>(items, request.Page, request.PageSize, latest.Count);
    }

    public async Task<Result<OpenServiceOrderListDto>> OpenServiceOrdersAsync(CancellationToken cancellationToken) =>
        new OpenServiceOrderListDto(await queries.OpenServiceOrdersWithoutQuoteAsync(cancellationToken));

    public async Task<Result<QuoteDto>> CreateAsync(Guid serviceOrderId, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(serviceOrderId, cancellationToken) is not { IsOpen: true } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        if (await estimates.ExistsForServiceOrderAsync(order.Id, cancellationToken))
        {
            return QuoteErrors.AlreadyExists;
        }

        var payment = await PaymentsAsync(cancellationToken);
        var estimate = Estimate.Open(await estimates.NextNumberAsync(cancellationToken), order.Id, payment.Methods, payment.MaxInstallments);
        var lines = new List<EstimateLine>();
        foreach (var item in order.ServiceItems)
        {
            lines.Add(new EstimateLine(Guid.CreateVersion7(), "Service", item.ServiceId, item.Description, item.UnitPrice.Amount, item.Quantity, "Manual", false));
        }

        foreach (var item in order.PartItems)
        {
            var option = await queries.OptionAsync("Part", item.PartId, cancellationToken);
            lines.Add(new EstimateLine(Guid.CreateVersion7(), "Part", item.PartId, item.Description, item.UnitPrice.Amount, item.Quantity.Value, "Manual", option?.Fractional ?? false));
        }

        estimate.SaveDraft(lines, "Automatic", null, payment.Methods, payment.MaxInstallments, timeProvider.GetUtcNow());
        estimates.Add(estimate);
        return await CommitAndDtoAsync(estimate, cancellationToken);
    }

    public async Task<Result<QuoteDto>> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await estimates.GetByIdAsync(id, cancellationToken) is { } estimate ? await DtoAsync(estimate, cancellationToken) : QuoteErrors.NotFound;

    public async Task<Result<QuoteOptionsDto>> OptionsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var discounts = await DiscountsAsync(cancellationToken);
        var payment = await PaymentsAsync(cancellationToken);
        var order = await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken);
        var suggestions = new List<QuoteOptionDto>();
        foreach (var item in order?.ServiceItems.Select(item => ("Service", item.ServiceId)).Concat(order.PartItems.Select(item => ("Part", item.PartId))) ?? [])
        {
            if (await queries.OptionAsync(item.Item1, item.Item2, cancellationToken) is { } option)
            {
                suggestions.Add(option with { Reason = "Indicado no diagnóstico do mecânico." });
            }
        }

        return new QuoteOptionsDto(
            await queries.OptionsAsync("Package", cancellationToken),
            await queries.OptionsAsync("Service", cancellationToken),
            await queries.OptionsAsync("Part", cancellationToken),
            suggestions,
            discounts.Rules.Select(rule => new DiscountRuleDto(rule.Id, rule.Name, rule.Type, rule.Value, rule.Condition, rule.IsActive)).ToList(),
            await LimitAsync(discounts, cancellationToken),
            new PaymentSettingsDto(payment.Methods, payment.MaxInstallments, payment.InterestFreeInstallments, payment.MonthlyInterestPercent, payment.Fees.Select(fee => new PaymentFeeDto(fee.Method, fee.FeePercent, fee.SettlementDays)).ToList()),
            SettingsDefaults.ExpirationDays);
    }

    public async Task<Result<QuoteDto>> SaveDraftAsync(Guid id, QuoteDraftInput input, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        if (!estimate.IsEditable(now))
        {
            return QuoteErrors.Locked;
        }

        var lines = new List<EstimateLine>();
        foreach (var item in input.Items)
        {
            var option = Kinds.Contains(item.Kind) ? await queries.OptionAsync(item.Kind, item.ItemId, cancellationToken) : null;
            if (option is null)
            {
                return QuoteErrors.InvalidItem;
            }

            if (!EstimatePricing.IsValidQuantity(item.Quantity, option.Fractional))
            {
                return QuoteErrors.InvalidQuantity;
            }

            var previous = estimate.Lines.FirstOrDefault(line => line.Kind == item.Kind && line.ItemId == item.ItemId);
            lines.Add(new EstimateLine(previous?.Id ?? Guid.CreateVersion7(), item.Kind, item.ItemId, option.Name, Math.Max(0, item.UnitPrice ?? option.UnitPrice), item.Quantity, item.Source == "Ai" ? "Ai" : "Manual", option.Fractional));
        }

        var mode = input.DiscountMode is "Manual" or "None" ? input.DiscountMode : "Automatic";
        var manual = input.ManualDiscount is { } discount ? new ManualDiscount(discount.Type, discount.Value, discount.Reason ?? string.Empty) : null;
        if (mode == "Manual" && manual is not null && manual.Value <= 0)
        {
            return QuoteErrors.InvalidDiscount;
        }

        var payment = await PaymentsAsync(cancellationToken);
        var methods = input.Payment?.Methods.Where(Methods.Contains).Distinct().ToList() ?? estimate.PaymentMethods;
        var installments = input.Payment is { } plan ? Math.Min(Math.Max(1, plan.MaxInstallments), payment.MaxInstallments) : estimate.MaxInstallments;
        if (estimate.SaveDraft(lines, mode, manual, methods, installments, now).Error is { } saveError)
        {
            return saveError;
        }

        return await CommitAndDtoAsync(estimate, cancellationToken);
    }

    public async Task<Result<DiscountApprovalDto>> RequestApprovalAsync(Guid id, string type, decimal value, string reason, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        if (!estimate.IsEditable(now))
        {
            return QuoteErrors.Locked;
        }

        if (!EstimatePricing.IsValidDiscount(type, value))
        {
            return QuoteErrors.InvalidDiscount;
        }

        if ((reason ?? string.Empty).Trim().Length < Estimate.MinReasonLength)
        {
            return QuoteErrors.DiscountReason;
        }

        var discounts = await DiscountsAsync(cancellationToken);
        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var totals = EstimatePricing.Price(estimate.Lines, "Manual", new ManualDiscount(type, value, reason!), discounts.Rules, context?.Facts ?? new CustomerFacts(false, false, false), await LimitAsync(discounts, cancellationToken));
        if (!totals.ExceedsLimit)
        {
            return QuoteErrors.ApprovalNotNeeded;
        }

        estimate.RequestApproval(type, value, reason!, await CurrentNameAsync(cancellationToken), now);
        await notifications.PublishAsync("DiscountApproval", $"Desconto do orçamento nº {estimate.Number} aguarda aprovação", reason!.Trim(), "/quotes", false, null, cancellationToken);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return Approval(estimate.Approval!);
    }

    public async Task<Result<DiscountApprovalDto>> DecideApprovalAsync(Guid id, Guid approvalId, bool approve, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        if (estimate.DecideApproval(approvalId, approve, await CurrentNameAsync(cancellationToken)).Error is { } decideError)
        {
            return decideError;
        }

        audit.Add(AuditEntry.Record(timeProvider.GetUtcNow(), currentUser.UserId, await CurrentNameAsync(cancellationToken), "discounts", approve ? "Desconto aprovado" : "Desconto recusado", $"Orçamento nº {estimate.Number}", estimate.Approval?.Reason ?? string.Empty));

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return Approval(estimate.Approval!);
    }

    public async Task<Result<MessageDraftDto>> MessageDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate || await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken) is not { } context)
        {
            return QuoteErrors.NotFound;
        }

        var totals = Totals(estimate, await DiscountsAsync(cancellationToken), context.Facts, 100);
        return new MessageDraftDto(Message(context, totals.Total, 7, null));
    }

    public async Task<Result<SendQuoteResultDto>> SendAsync(Guid id, string channel, int expiresInDays, string message, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate || await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken) is not { } context)
        {
            return QuoteErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        if (!estimate.IsEditable(now))
        {
            return QuoteErrors.Locked;
        }

        if (!Channels.Contains(channel))
        {
            return QuoteErrors.InvalidChannel;
        }

        if (!SettingsDefaults.ExpirationDays.Contains(expiresInDays))
        {
            return QuoteErrors.InvalidExpiration;
        }

        if (string.IsNullOrWhiteSpace(message) || message.Length > MaxMessageLength)
        {
            return QuoteErrors.InvalidMessage;
        }

        if ((channel == "WhatsApp" && !(context.ConsentWhatsApp && context.Phone is not null)) || (channel == "Email" && !(context.ConsentEmail && context.Email is not null)))
        {
            return QuoteErrors.NoConsent;
        }

        var discounts = await DiscountsAsync(cancellationToken);
        var totals = EstimatePricing.Price(estimate.Lines, estimate.DiscountMode, estimate.ManualDiscount, discounts.Rules, context.Facts, await LimitAsync(discounts, cancellationToken));
        if (SendBlock(estimate, totals) is { } block)
        {
            return block;
        }

        var synced = await SyncServiceOrderAsync(estimate, totals.Total, cancellationToken);
        if (synced.Error is { } syncError)
        {
            return syncError;
        }

        var token = InvitationToken.Generate();
        var link = links.Portal(token);
        estimate.Deliver(channel, link, InvitationToken.Hash(token), now.AddDays(expiresInDays));
        if (await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken) is { } order)
        {
            var issuer = new DocumentIssuer(documents, settings, timeProvider);
            var vehicle = $"{context.LicensePlate} · {context.Vehicle}";
            await issuer.IssueAsync(DocumentIssuer.ServiceOrderKind, order, context.CustomerName, vehicle, null, null, null, cancellationToken);
            await issuer.IssueAsync(DocumentIssuer.QuoteKind, order, context.CustomerName, vehicle, estimate, this, null, cancellationToken);
        }

        var body = message.Contains(link, StringComparison.Ordinal) ? message : $"{message.Trim()} {link}";
        if (channel == "Email")
        {
            var sent = await emailSender.SendAsync(new EmailMessage(context.Email!, $"Orçamento nº {estimate.Number} · {context.Vehicle}", $"<p>{System.Net.WebUtility.HtmlEncode(body)}</p><p><a href=\"{link}\">Ver e aprovar o orçamento</a></p>"), cancellationToken);
            estimate.MarkDelivery(sent, sent ? null : "Não foi possível enviar o e-mail. Tente outro canal.", now);
        }
        else
        {
            estimate.MarkDelivery(true, null, now);
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        var whatsApp = channel == "WhatsApp" ? $"https://wa.me/55{context.Phone}?text={Uri.EscapeDataString(body)}" : null;
        return new SendQuoteResultDto(Delivery(estimate.Delivery!), whatsApp);
    }

    public async Task<Result<QuoteDeliveryDto>> DeliveryAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        return estimate.Delivery is { } delivery ? Delivery(delivery) : QuoteErrors.NoDelivery;
    }

    public async Task<Result<QuoteDeliveryDto>> RetryAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { Delivery: { } delivery } estimate || await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken) is not { } context)
        {
            return QuoteErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        if (estimate.EnsureRetry(now).Error is { } retryError)
        {
            return retryError;
        }

        var totals = Totals(estimate, await DiscountsAsync(cancellationToken), context.Facts, 100);
        var channel = delivery.Channel == "Email" && !(context.ConsentEmail && context.Email is not null) ? "Link" : delivery.Channel;
        var result = await SendAsync(id, channel, 7, Message(context, totals.Total, 7, null), cancellationToken);
        return result.Error is { } error ? error : result.Value.Delivery;
    }

    public async Task<Result<QuoteDto>> ApproveAsync(Guid id, IReadOnlyCollection<Guid>? acceptedLineIds, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        if (estimate.Status != EstimateStatus.Pending)
        {
            return QuoteErrors.NotPending;
        }

        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var discounts = await DiscountsAsync(cancellationToken);
        var accepted = acceptedLineIds is null ? estimate.Lines : estimate.Lines.Where(line => acceptedLineIds.Contains(line.Id)).ToList();
        var totals = EstimatePricing.Price(accepted, estimate.DiscountMode, estimate.ManualDiscount, discounts.Rules, context?.Facts ?? new CustomerFacts(false, false, false), 100);
        if (accepted.Count == 0 || totals.Total <= 0)
        {
            return QuoteErrors.ZeroTotal;
        }

        estimate.Approve(accepted.Select(line => line.Id).ToList());
        var approved = await ApproveServiceOrderAsync(estimate, accepted, totals.Total, cancellationToken);
        if (approved.Error is { } approveError)
        {
            return approveError;
        }

        return await CommitAndDtoAsync(estimate, cancellationToken);
    }

    public async Task<Result<QuoteDto>> RejectAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        if (estimate.Reject(reason).Error is { } rejectError)
        {
            return rejectError;
        }

        if (await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken) is { Status: ServiceOrderStatus.WaitingApproval } order)
        {
            order.RejectQuote(reason, currentUser.UserId, timeProvider.GetUtcNow());
        }

        return await CommitAndDtoAsync(estimate, cancellationToken);
    }

    public async Task<Result<QuoteDto>> NewVersionAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var latest = await estimates.GetLatestByNumberAsync(estimate.Number, cancellationToken);
        if (latest?.Id != estimate.Id)
        {
            return QuoteErrors.VersionNotAllowed;
        }

        var next = estimate.NextVersion();
        if (next.Error is { } versionError)
        {
            return versionError;
        }

        estimates.Add(next.Value);
        return await CommitAndDtoAsync(next.Value, cancellationToken);
    }

    public async Task<Result<QuoteVersionListDto>> VersionsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var discounts = await DiscountsAsync(cancellationToken);
        var versions = await queries.VersionsAsync(estimate.Number, cancellationToken);
        return new QuoteVersionListDto(versions.Select(version =>
        {
            var totals = Totals(version, discounts, context?.Facts, 100);
            return new QuoteVersionDto(version.Id, version.Version, version.Status, version.Lines.Select(Line).ToList(), totals.Services, totals.Parts, totals.Discount, totals.Total, version.RejectionReason, version.CreatedAt);
        }).ToList());
    }

    public async Task<QuoteDto> DtoAsync(Estimate estimate, CancellationToken cancellationToken)
    {
        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var totals = Totals(estimate, await DiscountsAsync(cancellationToken), context?.Facts, 100);
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
            new PaymentPlanDto(estimate.PaymentMethods, estimate.MaxInstallments),
            estimate.Approval is { } approval ? Approval(approval) : null,
            estimate.Delivery is { } delivery ? Delivery(delivery) : null,
            estimate.RejectionReason,
            totals.Services,
            totals.Parts,
            totals.Discount,
            totals.Total,
            estimate.UpdatedAt ?? estimate.CreatedAt);
    }

    public async Task<EstimateTotals> TotalsAsync(Estimate estimate, IReadOnlyCollection<EstimateLine>? lines, CancellationToken cancellationToken)
    {
        var context = await queries.ContextAsync(estimate.ServiceOrderId, cancellationToken);
        var discounts = await DiscountsAsync(cancellationToken);
        return EstimatePricing.Price(lines ?? estimate.Lines, estimate.DiscountMode, estimate.ManualDiscount, discounts.Rules, context?.Facts ?? new CustomerFacts(false, false, false), 100);
    }

    public static string? MaskPhone(string? phone) =>
        phone is { Length: >= 10 } ? $"({phone[..2]}) {phone[2]}••••-{phone[^4..]}" : null;

    public static string? MaskEmail(string? email) =>
        email?.Split('@') is [var name, var domain] && name.Length > 0 ? $"{name[0]}••••@{domain}" : null;

    private static Error? SendBlock(Estimate estimate, EstimateTotals totals)
    {
        if (estimate.Lines.Count == 0)
        {
            return QuoteErrors.Empty;
        }

        if (totals.Total <= 0)
        {
            return QuoteErrors.ZeroTotal;
        }

        if (estimate.Lines.Any(line => !EstimatePricing.IsValidQuantity(line.Quantity, line.Fractional)))
        {
            return QuoteErrors.InvalidQuantity;
        }

        if (estimate.DiscountMode != "Manual")
        {
            return null;
        }

        if (estimate.ManualDiscount is not { } manual || !EstimatePricing.IsValidDiscount(manual.Type, manual.Value) || manual.Reason.Trim().Length < Estimate.MinReasonLength)
        {
            return QuoteErrors.DiscountReason;
        }

        var approved = estimate.Approval is { Status: "Approved" } approval && approval.Type == manual.Type && approval.Value == manual.Value;
        return totals.ExceedsLimit && !approved ? QuoteErrors.ApprovalRequired : null;
    }

    private async Task<Result> SyncServiceOrderAsync(Estimate estimate, decimal total, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        if (order.Status != ServiceOrderStatus.InDiagnosis)
        {
            return order.Status == ServiceOrderStatus.Received ? order.StartDiagnosis(currentUser.UserId, timeProvider.GetUtcNow()) is { IsSuccess: true } ? await SyncServiceOrderAsync(estimate, total, cancellationToken) : ServiceOrderErrors.InvalidTransition : Result.Success();
        }

        var replaced = await ReplaceItemsAsync(order, estimate.Lines, cancellationToken);
        if (replaced.IsFailure)
        {
            return replaced;
        }

        var sent = order.SendQuote(ApprovalToken.FromRawToken(InvitationToken.Generate()).Value, currentUser.UserId, timeProvider.GetUtcNow());
        if (sent.IsFailure)
        {
            return sent;
        }

        order.SetQuotedTotal(Money.Create(total).Value);
        return Result.Success();
    }

    private async Task<Result> ApproveServiceOrderAsync(Estimate estimate, IReadOnlyCollection<EstimateLine> accepted, decimal total, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken) is not { } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        if (order.Status == ServiceOrderStatus.WaitingApproval && accepted.Count != estimate.Lines.Count)
        {
            order.RejectQuote("Aprovação parcial pelo cliente", currentUser.UserId, now);
        }

        if (order.Status == ServiceOrderStatus.Received)
        {
            order.StartDiagnosis(currentUser.UserId, now);
        }

        if (order.Status == ServiceOrderStatus.InDiagnosis)
        {
            var replaced = await ReplaceItemsAsync(order, accepted, cancellationToken);
            if (replaced.IsFailure)
            {
                return replaced;
            }

            var sent = order.SendQuote(ApprovalToken.FromRawToken(InvitationToken.Generate()).Value, currentUser.UserId, now);
            if (sent.IsFailure)
            {
                return sent;
            }
        }

        if (order.Status != ServiceOrderStatus.WaitingApproval)
        {
            return Result.Success();
        }

        var approved = await approvalPolicy.ApproveAsync(order, currentUser.UserId, now, cancellationToken);
        if (approved.IsFailure)
        {
            return approved;
        }

        order.SetQuotedTotal(Money.Create(total).Value);
        return Result.Success();
    }

    private async Task<Result> ReplaceItemsAsync(ServiceOrder order, IReadOnlyCollection<EstimateLine> lines, CancellationToken cancellationToken)
    {
        var serviceLines = new List<(Guid Id, int Quantity)>();
        var partLines = new List<(Guid Id, decimal Quantity)>();
        foreach (var line in lines)
        {
            if (line.Kind == "Package" && await packages.GetByIdAsync(line.ItemId, cancellationToken) is { } package)
            {
                serviceLines.AddRange(package.Items.Where(item => item.Kind == Domain.Catalog.Enums.CatalogItemKind.Service).Select(item => (item.ItemId, (int)decimal.Ceiling(item.Quantity.Value * line.Quantity))));
                partLines.AddRange(package.Items.Where(item => item.Kind == Domain.Catalog.Enums.CatalogItemKind.Part).Select(item => (item.ItemId, item.Quantity.Value * line.Quantity)));
            }
            else if (line.Kind == "Service")
            {
                serviceLines.Add((line.ItemId, (int)decimal.Ceiling(line.Quantity)));
            }
            else if (line.Kind == "Part")
            {
                partLines.Add((line.ItemId, line.Quantity));
            }
        }

        var serviceEntities = await services.ListByIdsAsync(serviceLines.Select(line => line.Id).Distinct().ToList(), cancellationToken);
        var partEntities = await parts.ListByIdsAsync(partLines.Select(line => line.Id).Distinct().ToList(), cancellationToken);
        var serviceItems = serviceLines.GroupBy(line => line.Id)
            .Select(group => (Service: serviceEntities.FirstOrDefault(service => service.Id == group.Key), Quantity: group.Sum(line => line.Quantity)))
            .Where(item => item.Service is not null)
            .Select(item => (item.Service!, item.Quantity))
            .ToList();
        var partItems = partLines.GroupBy(line => line.Id)
            .Select(group => (Part: partEntities.FirstOrDefault(part => part.Id == group.Key), Quantity: group.Sum(line => line.Quantity)))
            .Where(item => item.Part is not null)
            .Select(item => (item.Part!, Quantity.Create(item.Quantity).Value))
            .ToList();
        return order.ReplaceItems(serviceItems, partItems);
    }

    private async Task<Result<QuoteDto>> CommitAndDtoAsync(Estimate estimate, CancellationToken cancellationToken)
    {
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await DtoAsync(estimate, cancellationToken);
    }

    private static EstimateTotals Totals(Estimate estimate, DiscountSettings discounts, CustomerFacts? facts, decimal limit) =>
        EstimatePricing.Price(estimate.Lines, estimate.DiscountMode, estimate.ManualDiscount, discounts.Rules, facts ?? new CustomerFacts(false, false, false), limit);

    private Task<DiscountSettings> DiscountsAsync(CancellationToken cancellationToken) =>
        settings.GetAsync(SettingsKeys.Discounts, SettingsDefaults.Discounts, cancellationToken);

    private Task<PaymentSettings> PaymentsAsync(CancellationToken cancellationToken) =>
        settings.GetAsync(SettingsKeys.Payments, SettingsDefaults.Payments, cancellationToken);

    private async Task<decimal> LimitAsync(DiscountSettings discounts, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId || await users.GetByIdAsync(userId, cancellationToken) is not { } user)
        {
            return 0;
        }

        if (await profiles.GetAsync(userId, cancellationToken) is { UpdatedAt: not null } profile)
        {
            return profile.DiscountLimitPercent;
        }

        return discounts.Limits.FirstOrDefault(limit => limit.Role == user.Role.ToString())?.Percent ?? 0;
    }

    private async Task<string> CurrentNameAsync(CancellationToken cancellationToken) =>
        currentUser.UserId is { } userId && await users.GetByIdAsync(userId, cancellationToken) is { } user ? user.Name : "Sistema";

    private static string Message(EstimateContext context, decimal total, int days, string? link) =>
        $"Olá, {context.CustomerName.Split(' ')[0]}! O orçamento do seu {context.Vehicle} ({context.LicensePlate}) está pronto: {total.ToString("C", Brazil)}. " +
        $"Veja os itens, as fotos e aprove pelo link em até {days} dias. Qualquer dúvida, é só responder.{(link is null ? string.Empty : $" {link}")}";

    private static QuoteLineDto Line(EstimateLine line) => new(line.Id, line.Kind, line.ItemId, line.Name, line.UnitPrice, line.Quantity, line.Source, line.Fractional);

    private static DiscountApprovalDto Approval(DiscountApproval approval) =>
        new(approval.Id, approval.Status, new DiscountDto(approval.Type, approval.Value), approval.Reason, approval.RequestedBy, approval.RequestedAt, approval.DecidedBy);

    private static QuoteDeliveryDto Delivery(EstimateDelivery delivery) =>
        new(delivery.Id, delivery.Channel, delivery.Status, delivery.Link, delivery.ExpiresAt, delivery.SentAt, delivery.OpenedAt, delivery.FailureReason, delivery.Attempts);
}
