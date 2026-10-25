using Oficina.Application.Inventory.Dtos;

namespace Oficina.Api.Contracts.Inventory;

public sealed record InvoiceConfirmRequest(Guid? SupplierId, string InvoiceNumber, IReadOnlyList<InvoiceConfirmLine>? Lines);
