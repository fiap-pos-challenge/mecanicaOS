using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Vehicles.Dto;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Vehicles.Commands.UpdateVehicle;

public class UpdateVehicleCommandHandler(IVehicleRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateVehicleCommand, Result<VehicleDto>>
{
    public async Task<Result<VehicleDto>> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (vehicle is null)
        {
            return VehicleErrors.NotFound;
        }

        var year = VehicleYear.Create(request.Year);
        if (year.Error is not null)
        {
            return year.Error;
        }

        vehicle.ChangeDetails(request.Brand, request.Model, year.Value, TimeProvider.System.GetUtcNow());
        await unitOfWork.CommitAsync(cancellationToken);
        return vehicle.ToDto();
    }
}
