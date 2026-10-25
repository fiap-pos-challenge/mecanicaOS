using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Quotes.Entities;
using Oficina.Domain.Quotes.Errors;
using Oficina.Domain.Quotes.Repositories;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.Quotes.Queries.GetQuoteOptions;

public sealed class GetQuoteOptionsQueryHandler(IEstimateRepository estimates, IServiceOrderRepository serviceOrders, IQuoteQueries queries, QuotePricing pricing)
    : IRequestHandler<GetQuoteOptionsQuery, Result<QuoteOptionsDto>>
{
    public async Task<Result<QuoteOptionsDto>> Handle(GetQuoteOptionsQuery query, CancellationToken cancellationToken)
    {
        if (await estimates.GetByIdAsync(query.Id, cancellationToken) is not { } estimate)
        {
            return QuoteErrors.NotFound;
        }

        var discounts = await pricing.DiscountsAsync(cancellationToken);
        var payment = await pricing.PaymentsAsync(cancellationToken);
        var order = await serviceOrders.GetByIdAsync(estimate.ServiceOrderId, cancellationToken);
        var indicated = order?.ServiceItems.Select(item => (Kind: EstimateLineKind.Service, Id: item.ServiceId)).Concat(order.PartItems.Select(item => (Kind: EstimateLineKind.Part, Id: item.PartId))) ?? [];
        var suggestions = new List<QuoteOptionDto>();
        foreach (var (kind, id) in indicated)
        {
            if (await queries.OptionAsync(kind, id, cancellationToken) is { } option)
            {
                suggestions.Add(option with { Reason = QuoteTexts.DiagnosisSuggestion });
            }
        }

        return new QuoteOptionsDto(
            await queries.OptionsAsync(EstimateLineKind.Package, cancellationToken),
            await queries.OptionsAsync(EstimateLineKind.Service, cancellationToken),
            await queries.OptionsAsync(EstimateLineKind.Part, cancellationToken),
            suggestions,
            discounts.Rules.Select(rule => new DiscountRuleDto(rule.Id, rule.Name, rule.Type, rule.Value, rule.Condition, rule.IsActive)).ToList(),
            await pricing.LimitAsync(discounts, cancellationToken),
            new PaymentSettingsDto(payment.Methods, payment.MaxInstallments, payment.InterestFreeInstallments, payment.MonthlyInterestPercent, payment.Fees.Select(fee => new PaymentFeeDto(fee.Method, fee.FeePercent, fee.SettlementDays)).ToList()),
            SettingsDefaults.ExpirationDays);
    }
}
