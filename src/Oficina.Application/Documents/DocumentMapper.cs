using Oficina.Application.Documents.Dtos;
using Oficina.Domain.Documents.Entities;

namespace Oficina.Application.Documents;

public static class DocumentMapper
{
    public static DocumentDto ToDto(this IssuedDocument document)
    {
        var snapshot = document.Snapshot;
        return new DocumentDto(document.Kind, document.Code, snapshot.Number, snapshot.Version, document.IssuedAt, snapshot.ShopName, snapshot.CustomerName, snapshot.Vehicle,
            snapshot.Lines.Select(line => new DocumentLineDto(line.Name, line.Quantity, line.UnitPrice, line.Total)).ToList(),
            snapshot.ServicesTotal, snapshot.PartsTotal, snapshot.DiscountTotal, snapshot.Total, snapshot.PaidAt, snapshot.PaymentMethods);
    }
}
