using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Quotes.Entities;

namespace Oficina.Application.Quotes.Queries;

public interface IQuoteQueries
{
    Task<EstimateContext?> ContextAsync(Guid serviceOrderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Estimate>> LatestAsync(string? status, CancellationToken cancellationToken);

    Task<IReadOnlyList<Estimate>> VersionsAsync(long number, CancellationToken cancellationToken);

    Task<IReadOnlyList<OpenServiceOrderDto>> OpenServiceOrdersWithoutQuoteAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<QuoteOptionDto>> OptionsAsync(string kind, CancellationToken cancellationToken);

    Task<QuoteOptionDto?> OptionAsync(string kind, Guid itemId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, EstimateContext>> ContextsAsync(IReadOnlyCollection<Guid> serviceOrderIds, CancellationToken cancellationToken);
}
