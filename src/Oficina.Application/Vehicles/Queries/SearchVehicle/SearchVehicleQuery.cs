using MediatR;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Queries.SearchVehicle;

public sealed record SearchVehicleQuery(string LicensePlate) : IRequest<Result<VehicleDto>>;
