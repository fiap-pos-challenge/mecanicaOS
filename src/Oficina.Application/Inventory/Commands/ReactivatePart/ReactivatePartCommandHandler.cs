using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Commands.ReactivatePart;

public sealed class ReactivatePartCommandHandler(IPartRepository parts, IUnitOfWork unitOfWork)
    : IRequestHandler<ReactivatePartCommand, Result>
{
    public async Task<Result> Handle(ReactivatePartCommand command, CancellationToken cancellationToken)
    {
        if (await parts.GetByIdAsync(command.Id, cancellationToken) is not { } part)
        {
            return InventoryErrors.NotFound;
        }

        var reactivated = part.Reactivate();
        if (reactivated.IsFailure)
        {
            return reactivated;
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
