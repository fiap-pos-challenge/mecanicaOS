using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.Application.Catalog.Commands;

public sealed record PartValues(PartCode Code, Money UnitPrice, Money Cost)
{
    public static Result<PartValues> Create(string code, decimal unitPrice, decimal cost)
    {
        var partCode = PartCode.Create(code);
        var price = Money.Create(unitPrice);
        var partCost = Money.Create(cost);
        if ((partCode.Error ?? price.Error ?? partCost.Error) is { } error)
        {
            return error;
        }

        return new PartValues(partCode.Value, price.Value, partCost.Value);
    }
}
