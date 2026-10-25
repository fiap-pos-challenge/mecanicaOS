using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Catalog.Commands.UpdatePart;

public sealed class UpdatePartCommandHandler(
    IPartRepository parts,
    IPriceChangeRepository priceChanges,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<UpdatePartCommand, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(UpdatePartCommand command, CancellationToken cancellationToken)
    {
        if (await parts.GetByIdAsync(command.Id, cancellationToken) is not { } part)
        {
            return InventoryErrors.NotFound;
        }

        var values = PartValues.Create(command.Code, command.UnitPrice, command.Cost);
        if (values.Error is { } invalidValues)
        {
            return invalidValues;
        }

        if (await parts.CodeExistsAsync(values.Value.Code, part.Id, cancellationToken))
        {
            return InventoryErrors.DuplicatePartCode;
        }

        var (oldCost, oldPrice) = (part.Cost, part.UnitPrice);
        var changed = part.ChangeDetails(values.Value.Code, command.Name, command.Type, command.UnitOfMeasure, values.Value.UnitPrice, values.Value.Cost, command.MinimumStock, command.Supplier, command.Barcode);
        if (changed.Error is { } invalidPart)
        {
            return invalidPart;
        }

        var now = timeProvider.GetUtcNow();
        priceChanges.Add(PriceChange.Record(CatalogItemKind.Part, part.Id, PriceField.Cost, oldCost, part.Cost, currentUser.UserId, now));
        priceChanges.Add(PriceChange.Record(CatalogItemKind.Part, part.Id, PriceField.UnitPrice, oldPrice, part.UnitPrice, currentUser.UserId, now));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return part.ToDto(includeCost: true);
    }
}
