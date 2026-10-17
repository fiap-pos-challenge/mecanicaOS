using MediatR;
using Oficina.Application.Inventory.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Inventory.Queries.ReadInvoice;

public sealed record ReadInvoiceQuery(string FileName, string ContentType, string Content) : IRequest<Result<InvoiceDraftDto>>;
