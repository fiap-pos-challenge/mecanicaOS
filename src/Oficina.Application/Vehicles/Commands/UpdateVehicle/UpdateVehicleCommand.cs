using MediatR;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Commands.UpdateVehicle;

public sealed record UpdateVehicleCommand(Guid Id, string Model, string Brand, int Year) : IRequest<Result<VehicleDto>>;
