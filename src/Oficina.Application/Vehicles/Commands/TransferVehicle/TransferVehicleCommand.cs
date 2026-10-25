using MediatR;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Commands.TransferVehicle;

public sealed record TransferVehicleCommand(Guid Id, Guid NewCustomerId) : IRequest<Result<VehicleDto>>;
