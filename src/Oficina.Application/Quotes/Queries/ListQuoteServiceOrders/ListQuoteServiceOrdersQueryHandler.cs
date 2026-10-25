using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Queries.ListQuoteServiceOrders;

public sealed class ListQuoteServiceOrdersQueryHandler(IQuoteQueries queries) : IRequestHandler<ListQuoteServiceOrdersQuery, Result<OpenServiceOrderListDto>>
{
    public async Task<Result<OpenServiceOrderListDto>> Handle(ListQuoteServiceOrdersQuery query, CancellationToken cancellationToken) =>
        new OpenServiceOrderListDto(await queries.OpenServiceOrdersWithoutQuoteAsync(cancellationToken));
}
