using MediatR;
using Oficina.Application.Documents.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Documents.Queries.GetDocumentPdf;

public sealed record GetDocumentPdfQuery(string Code, Guid? ServiceOrderId) : IRequest<Result<DocumentDownloadDto>>;
