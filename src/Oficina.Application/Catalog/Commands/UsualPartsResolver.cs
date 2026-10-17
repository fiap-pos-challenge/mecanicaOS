using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Validation;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Catalog.Commands;

public static class UsualPartsResolver
{
    public static async Task<Result<IReadOnlyList<(Guid PartId, Quantity Quantity)>>> ResolveAsync(
        IPartRepository parts,
        IReadOnlyList<UsualPartInput> inputs,
        CancellationToken cancellationToken)
    {
        var resolved = CatalogItemRules.ToUsualParts(inputs, CatalogErrors.InvalidUsualParts);
        if (resolved.Error is { } invalid)
        {
            return invalid;
        }

        var ids = inputs.Select(input => input.PartId).Distinct().ToList();
        var found = await parts.ListByIdsAsync(ids, cancellationToken);
        return found.Count == ids.Count ? resolved : CatalogErrors.InvalidUsualParts;
    }
}
