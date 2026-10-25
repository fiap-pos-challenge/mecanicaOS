using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Catalog.Commands.DeactivatePart;

public sealed class DeactivatePartCommandHandler(IPartRepository parts, IPackageRepository packages, IUnitOfWork unitOfWork)
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

        var blocking = await packages.ListActiveNamesUsingAsync(CatalogItemKind.Part, part.Id, cancellationToken);
        if (blocking.Count > 0)
        {
            return CatalogErrors.UsedByActivePackage(blocking);
        }

        return await unitOfWork.CommitAsync(cancellationToken);
    }
}
