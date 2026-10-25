using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Commands.ConfirmInvoice;

public sealed record ConfirmInvoiceCommand(Guid? SupplierId, string InvoiceNumber, IReadOnlyList<InvoiceConfirmLine> Lines) : IRequest<Result<InvoiceConfirmedDto>>;
