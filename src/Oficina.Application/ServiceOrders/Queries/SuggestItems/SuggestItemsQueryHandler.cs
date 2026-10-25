using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.SuggestItems;

public sealed class SuggestItemsQueryHandler(IServiceOrderBoardQueries queries) : IRequestHandler<SuggestItemsQuery, Result<SuggestedItemListDto>>
{
    public async Task<Result<SuggestedItemListDto>> Handle(SuggestItemsQuery query, CancellationToken cancellationToken) =>
        new SuggestedItemListDto(await queries.SuggestAsync(query.Description, cancellationToken));
}
