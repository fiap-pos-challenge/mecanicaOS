using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.DeactivatePart;

public sealed class DeactivatePartCommandHandler(IPartRepository parts, IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivatePartCommand, Result>
{
    public async Task<Result> Handle(DeactivatePartCommand command, CancellationToken cancellationToken)
    {
        if (await parts.GetByIdAsync(command.Id, cancellationToken) is not { } part)
        {
            return InventoryErrors.NotFound;
        }

        var deactivated = part.Deactivate();
        if (deactivated.IsFailure)
        {
            return deactivated;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
