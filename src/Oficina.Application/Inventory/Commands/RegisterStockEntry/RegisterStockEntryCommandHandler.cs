using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.RegisterStockEntry;

public sealed class RegisterStockEntryCommandHandler(IPartRepository parts, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterStockEntryCommand, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(RegisterStockEntryCommand command, CancellationToken cancellationToken)
    {
        if (await parts.GetByIdAsync(command.PartId, cancellationToken) is not { } part)
        {
            return InventoryErrors.NotFound;
        }

        var quantity = Quantity.Create(command.Quantity);
        if (quantity.Error is { } invalidQuantity)
        {
            return invalidQuantity;
        }

        var registered = part.RegisterEntry(quantity.Value, command.Reason, currentUser.UserId);
        if (registered.Error is { } invalidEntry)
        {
            return invalidEntry;
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return part.ToDto();
    }
}
