using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Application.Inventory.Queries.LookupBarcode;

public sealed class LookupBarcodeQueryHandler(IInventoryQueries queries) : IRequestHandler<LookupBarcodeQuery, Result<StockItemDto>>
{
    public async Task<Result<StockItemDto>> Handle(LookupBarcodeQuery request, CancellationToken cancellationToken)
    {
        var digits = new string(request.Barcode.Where(char.IsAsciiDigit).ToArray());
        return await queries.ByBarcodeAsync(digits, cancellationToken) is { } item ? item : InventoryErrors.UnknownBarcode;
    }
}
