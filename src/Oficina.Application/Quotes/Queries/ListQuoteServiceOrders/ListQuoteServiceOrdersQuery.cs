using MediatR;
using Oficina.Application.Quotes.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Quotes.Queries.ListQuoteServiceOrders;

public sealed record ListQuoteServiceOrdersQuery : IRequest<Result<OpenServiceOrderListDto>>;
