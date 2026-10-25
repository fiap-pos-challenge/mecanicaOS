using MediatR;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Queries.ListCustomerVehicles;

public sealed record ListCustomerVehiclesQuery(Guid CustomerId) : IRequest<Result<VehicleListDto>>;
