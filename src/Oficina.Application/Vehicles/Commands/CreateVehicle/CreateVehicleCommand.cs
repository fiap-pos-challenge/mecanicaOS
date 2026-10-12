using MediatR;
using Oficina.Application.Vehicles.Dto;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Vehicles.Commands.CreateVehicle;

public sealed record CreateVehicleCommand(Guid CustomerId, string Plate,
    string Brand, string Model, int Year) : IRequest<Result<VehicleDto>>{}
