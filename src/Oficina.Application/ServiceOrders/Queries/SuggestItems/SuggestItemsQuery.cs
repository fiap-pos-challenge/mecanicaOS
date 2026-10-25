using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.SuggestItems;

public sealed record SuggestItemsQuery(Guid Id, string Description) : IRequest<Result<SuggestedItemListDto>>;
