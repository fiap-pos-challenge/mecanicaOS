using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Entities;

namespace Oficina.Domain.ServiceOrders.ValueObjects;

public sealed record QuoteScope(IReadOnlyCollection<(Service Service, int Quantity)> Services, IReadOnlyCollection<(Part Part, Quantity Quantity)> Parts, Money Total);
