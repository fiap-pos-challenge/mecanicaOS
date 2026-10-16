using MediatR;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Commands.DeleteVehicle;

public sealed record DeleteVehicleCommand(Guid Id) : IRequest<Result>;
