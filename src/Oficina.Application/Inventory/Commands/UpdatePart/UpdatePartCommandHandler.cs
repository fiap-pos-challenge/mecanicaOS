using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.UpdatePart;

public sealed class UpdatePartCommandHandler(IPartRepository parts, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePartCommand, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(UpdatePartCommand command, CancellationToken cancellationToken)
    {
        if (await parts.GetByIdAsync(command.Id, cancellationToken) is not { } part)
        {
            return InventoryErrors.NotFound;
        }

        var unitPrice = Money.Create(command.UnitPrice);
        if (unitPrice.Error is { } invalidPrice)
        {
            return invalidPrice;
        }

        var changed = part.ChangeDetails(command.Name, command.Type, command.UnitOfMeasure, unitPrice.Value, command.MinimumStock);
        if (changed.Error is { } invalidPart)
        {
            return invalidPart;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return part.ToDto();
    }
}
