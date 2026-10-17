using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;

namespace Oficina.Application.Vehicles.Commands.TransferVehicle;

public sealed class TransferVehicleCommandHandler(
    IVehicleRepository vehicles,
    ICustomerRepository customers,
    IUnitOfWork unitOfWork) : IRequestHandler<TransferVehicleCommand, Result<VehicleDto>>
{
    public async Task<Result<VehicleDto>> Handle(TransferVehicleCommand command, CancellationToken cancellationToken)
    {
        if (await vehicles.GetByIdAsync(command.Id, cancellationToken) is not { } vehicle)
        {
            return VehicleErrors.NotFound;
        }

        if (await customers.GetByIdAsync(command.NewCustomerId, cancellationToken) is null)
        {
            return CustomerErrors.NotFound;
        }

        if (vehicle.TransferTo(command.NewCustomerId).Error is { } invalidTransfer)
        {
            return invalidTransfer;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return vehicle.ToDto();
    }
}
