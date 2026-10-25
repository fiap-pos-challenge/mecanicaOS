using MediatR;
using Oficina.Application.Documents.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Documents.Queries.GetDocument;

public sealed record GetDocumentQuery(string Code, Guid? ServiceOrderId) : IRequest<Result<DocumentDto>>;
