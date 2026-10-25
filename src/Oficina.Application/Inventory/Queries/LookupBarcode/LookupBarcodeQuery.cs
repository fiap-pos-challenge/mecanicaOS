using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.LookupBarcode;

public sealed record LookupBarcodeQuery(string Barcode) : IRequest<Result<StockItemDto>>;
