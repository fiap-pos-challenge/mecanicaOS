using MediatR;
using Oficina.Application.Vehicles.Dto;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Queries.GetById;

public sealed record GetVehicleByIdQuery(Guid Id) : IRequest<Result<VehicleDto>>;
