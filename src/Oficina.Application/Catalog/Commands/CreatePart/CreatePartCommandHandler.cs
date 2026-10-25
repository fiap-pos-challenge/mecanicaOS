using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Mappers;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Enums;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Catalog.Commands.CreatePart;

public sealed class CreatePartCommandHandler(
    IPartRepository parts,
    IPriceChangeRepository priceChanges,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CreatePartCommand, Result<PartDto>>
{
    public async Task<Result<PartDto>> Handle(CreatePartCommand command, CancellationToken cancellationToken)
    {
        var values = PartValues.Create(command.Code, command.UnitPrice, command.Cost);
        if (values.Error is { } invalidValues)
        {
            return invalidValues;
        }

        if (await parts.CodeExistsAsync(values.Value.Code, null, cancellationToken))
        {
            return InventoryErrors.DuplicatePartCode;
        }

        var part = Part.Create(values.Value.Code, command.Name, command.Type, command.UnitOfMeasure, values.Value.UnitPrice, values.Value.Cost, command.MinimumStock, command.Supplier, command.Barcode);
        if (part.Error is { } invalidPart)
        {
            return invalidPart;
        }

        parts.Add(part.Value);
        var now = timeProvider.GetUtcNow();
        priceChanges.Add(PriceChange.Record(CatalogItemKind.Part, part.Value.Id, PriceField.Cost, null, part.Value.Cost, currentUser.UserId, now));
        priceChanges.Add(PriceChange.Record(CatalogItemKind.Part, part.Value.Id, PriceField.UnitPrice, null, part.Value.UnitPrice, currentUser.UserId, now));
        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return part.Value.ToDto(includeCost: true);
    }
}
