using MediatR;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.ServiceOrders.Commands.OpenServiceOrder;

public sealed record OpenServiceOrderCommand(Guid CustomerId, Guid VehicleId, string ProblemDescription, DateTimeOffset? PromisedAt, int? Mileage)
    : IRequest<Result<ServiceOrderDetailDto>>;
