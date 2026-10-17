using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Invoices;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;

namespace Oficina.Application.Inventory.Queries.ReadInvoice;

public sealed class ReadInvoiceQueryHandler(IPartRepository parts, IInventoryQueries queries) : IRequestHandler<ReadInvoiceQuery, Result<InvoiceDraftDto>>
{
    public async Task<Result<InvoiceDraftDto>> Handle(ReadInvoiceQuery request, CancellationToken cancellationToken)
    {
        if (NfeReader.Read(request.FileName, request.Content) is not { } invoice)
        {
            return InventoryErrors.InvalidInvoiceFile;
        }

        var supplier = (await queries.SuppliersAsync(cancellationToken)).Items.FirstOrDefault(item => item.Document is not null && item.Document == invoice.SupplierDocument);
        var lines = new List<InvoiceLineDto>();
        foreach (var line in invoice.Lines)
        {
            var part = line.Barcode is { } barcode ? await parts.GetByBarcodeAsync(barcode, cancellationToken) : null;
            lines.Add(new InvoiceLineDto(line.Description, line.Barcode, part?.Id, line.Quantity, line.UnitCost));
        }

        return new InvoiceDraftDto(supplier?.Name ?? invoice.SupplierName, invoice.SupplierDocument, supplier?.Id, invoice.Number, invoice.IssuedAt, lines);
    }
}
