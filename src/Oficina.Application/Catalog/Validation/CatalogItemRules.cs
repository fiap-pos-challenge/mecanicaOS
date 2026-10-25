using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Application.Catalog.Validation;

public static class CatalogItemRules
{
    public static Result<IReadOnlyList<(Guid PartId, Quantity Quantity)>> ToUsualParts(IReadOnlyList<UsualPartInput> inputs, Error invalid)
    {
        var parts = new List<(Guid PartId, Quantity Quantity)>();
        foreach (var input in inputs)
        {
            if (Quantity.Create(input.Quantity).Error is not null)
            {
                return invalid;
            }

            parts.Add((input.PartId, Quantity.Create(input.Quantity).Value));
        }

        return parts;
    }
}
