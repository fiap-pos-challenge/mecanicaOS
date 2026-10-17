using Oficina.Domain.Common.ValueObjects;

namespace Oficina.Domain.Catalog.Entities;

public sealed class ServiceUsualPart
{
    private ServiceUsualPart(Guid id, Guid partId, Quantity quantity)
    {
        Id = id;
        PartId = partId;
        Quantity = quantity;
    }

    public Guid Id { get; private init; }

    public Guid PartId { get; private init; }

    public Quantity Quantity { get; private init; }

    internal static ServiceUsualPart Create(Guid partId, Quantity quantity) => new(Guid.CreateVersion7(), partId, quantity);
}
