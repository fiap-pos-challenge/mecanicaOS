using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Queries.GetWork;

public sealed record GetWorkQuery(Guid Id) : IRequest<Result<MechanicOrderDto>>;
