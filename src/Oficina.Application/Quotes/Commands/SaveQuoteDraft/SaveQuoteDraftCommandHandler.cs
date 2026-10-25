using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Presenters;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Quotes.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;

namespace Oficina.Application.Quotes.Commands.SaveQuoteDraft;

public sealed class SaveQuoteDraftCommandHandler(
    IEstimateRepository estimates,
    IQuoteQueries queries,
    QuotePricing pricing,
    QuotePresenter presenter,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<SaveQuoteDraftCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(SaveQuoteDraftCommand command, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(command.Id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var now = timeProvider.GetUtcNow();
        if (estimate.EnsureEditable(now).Error is { } lockedError)
        {
            return lockedError;
        }

        var lines = new List<EstimateLine>();
        foreach (var item in command.Draft.Items)
        {
            var option = EstimateLineKind.All.Contains(item.Kind) ? await queries.OptionAsync(item.Kind, item.ItemId, cancellationToken) : null;
            if (option is null)
            {
                return QuoteErrors.InvalidItem;
            }

            var previous = estimate.Lines.FirstOrDefault(line => line.Kind == item.Kind && line.ItemId == item.ItemId);
            lines.Add(new EstimateLine(previous?.Id ?? Guid.CreateVersion7(), item.Kind, item.ItemId, option.Name, Math.Max(0, item.UnitPrice ?? option.UnitPrice), item.Quantity, EstimateLineSource.Normalize(item.Source), option.Fractional));
        }

        var draft = command.Draft;
        var manual = draft.ManualDiscount is { } discount ? new ManualDiscount(discount.Type, discount.Value, discount.Reason ?? string.Empty) : null;
        var payment = await pricing.PaymentsAsync(cancellationToken);
        IReadOnlyCollection<string> methods = draft.Payment?.Methods.Where(PaymentMethod.All.Contains).Distinct().ToList() ?? estimate.PaymentMethods;
        var installments = draft.Payment is { } plan ? Math.Min(Math.Max(1, plan.MaxInstallments), payment.MaxInstallments) : estimate.MaxInstallments;
        if (estimate.SaveDraft(lines, draft.DiscountMode, manual, methods, installments, now).Error is { } saveError)
        {
            return saveError;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await presenter.DtoAsync(estimate, cancellationToken);
    }
}
