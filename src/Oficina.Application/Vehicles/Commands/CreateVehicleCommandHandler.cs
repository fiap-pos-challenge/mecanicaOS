using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Vehicles.Commands.CreateVehicle;
using Oficina.Application.Vehicles.Dto;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Vehicles.Commands;

public sealed class CreateVehicleCommandHandler(IVehicleRepository vehicles, ICustomerRepository customers, IUnitOfWork unitOfWork) : IRequestHandler<CreateVehicleCommand, Result<VehicleDto>>
{
    public async Task<Result<VehicleDto>> Handle(CreateVehicleCommand command, CancellationToken cancellationToken)
    {

    if (await customers.GetByIdAsync(command.CustomerId, cancellationToken) is null)
    {
        return CustomerErrors.NotFound;
    }

    Result<LicensePlate> licensePlate = LicensePlate.Create(command.Plate);
    Result<VehicleYear> vehicleYear =  VehicleYear.Create(command.Year);

    if ((licensePlate.Error ?? vehicleYear.Error) is {} invalidData)
    {
        return invalidData;
    }

    if (await vehicles.GetByLicensePlate(licensePlate.Value, cancellationToken) is not null)
    {
        return VehicleErrors.DuplicatedLicensePlate;
    }

    Result<Vehicle> vehicle = Vehicle.Create(command.CustomerId, licensePlate.Value, command.Brand, command.Model, vehicleYear.Value, TimeProvider.System.GetLocalNow());

    if (vehicle.Error is {} invalidVehicle)
    {
        return invalidVehicle;
    }

    vehicles.Add(vehicle.Value);
    if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
    {
        return commitError;
    }

    return vehicle.Value.ToDto();
    }
}
