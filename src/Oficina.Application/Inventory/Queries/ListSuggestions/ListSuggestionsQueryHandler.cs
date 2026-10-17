using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ListSuggestions;

public sealed class ListSuggestionsQueryHandler(IInventoryQueries queries) : IRequestHandler<ListSuggestionsQuery, Result<PurchaseSuggestionListDto>>
{
    public async Task<Result<PurchaseSuggestionListDto>> Handle(ListSuggestionsQuery request, CancellationToken cancellationToken)
    {
        return await queries.SuggestionsAsync(cancellationToken);
    }
}
