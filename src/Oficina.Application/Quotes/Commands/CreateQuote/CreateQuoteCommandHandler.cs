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
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;

namespace Oficina.Application.Quotes.Commands.CreateQuote;

public sealed class CreateQuoteCommandHandler(
    IEstimateRepository estimates,
    IServiceOrderRepository serviceOrders,
    IQuoteQueries queries,
    QuotePricing pricing,
    QuotePresenter presenter,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreateQuoteCommand, Result<QuoteDto>>
{
    public async Task<Result<QuoteDto>> Handle(CreateQuoteCommand command, CancellationToken cancellationToken)
    {
        if (await serviceOrders.GetByIdAsync(command.ServiceOrderId, cancellationToken) is not { IsOpen: true } order)
        {
            return ServiceOrderErrors.NotFound;
        }

        if (await estimates.ExistsForServiceOrderAsync(order.Id, cancellationToken))
        {
            return QuoteErrors.AlreadyExists;
        }

        var payment = await pricing.PaymentsAsync(cancellationToken);
        var estimate = Estimate.Open(await estimates.NextNumberAsync(cancellationToken), order.Id, payment.Methods, payment.MaxInstallments);
        var lines = order.ServiceItems
            .Select(item => new EstimateLine(Guid.CreateVersion7(), EstimateLineKind.Service, item.ServiceId, item.Description, item.UnitPrice.Amount, item.Quantity, EstimateLineSource.Manual, false))
            .ToList();
        foreach (var item in order.PartItems)
        {
            var option = await queries.OptionAsync(EstimateLineKind.Part, item.PartId, cancellationToken);
            lines.Add(new EstimateLine(Guid.CreateVersion7(), EstimateLineKind.Part, item.PartId, item.Description, item.UnitPrice.Amount, item.Quantity.Value, EstimateLineSource.Manual, option?.Fractional ?? false));
        }

        var saved = estimate.SaveDraft(lines, DiscountMode.Automatic, null, payment.Methods, payment.MaxInstallments, timeProvider.GetUtcNow());
        if (saved.Error is { } saveError)
        {
            return saveError;
        }

        estimates.Add(estimate);
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return await presenter.DtoAsync(estimate, cancellationToken);
    }
}
