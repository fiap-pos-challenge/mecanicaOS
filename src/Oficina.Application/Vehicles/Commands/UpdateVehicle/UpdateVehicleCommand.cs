using MediatR;
using Oficina.Application.Vehicles.Dto;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Commands.UpdateVehicle;

public record UpdateVehicleCommand(Guid Id, string Model, string Brand, int Year) : IRequest<Result<VehicleDto>>;
