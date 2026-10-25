using MediatR;
using Oficina.Application.Documents.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Documents.Queries.VerifyDocument;

public sealed record VerifyDocumentQuery(string Code) : IRequest<Result<DocumentVerificationDto>>;
