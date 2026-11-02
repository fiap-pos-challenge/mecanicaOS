using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.Services;

namespace Oficina.Application.Vehicles.Commands.DeleteVehicle;

public sealed class DeleteVehicleCommandHandler(IVehicleRepository vehicles, RegistrationDeletionPolicy deletionPolicy,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteVehicleCommand, Result>
{
    public async Task<Result> Handle(DeleteVehicleCommand request, CancellationToken cancellationToken)
    {
        if (await vehicles.GetByIdAsync(request.Id, cancellationToken) is not { } vehicle)
        {
            return VehicleErrors.NotFound;
        }

        var canDelete = await deletionPolicy.EnsureCanDeleteVehicleAsync(vehicle, cancellationToken);
        if (canDelete.IsFailure)
        {
            return canDelete;
        }

        vehicles.Remove(vehicle);
        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
