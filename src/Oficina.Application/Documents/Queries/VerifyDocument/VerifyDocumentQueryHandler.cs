using MediatR;
using Oficina.Application.Documents.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Documents.Repositories;

namespace Oficina.Application.Documents.Queries.VerifyDocument;

public sealed class VerifyDocumentQueryHandler(IDocumentRepository documents) : IRequestHandler<VerifyDocumentQuery, Result<DocumentVerificationDto>>
{
    public async Task<Result<DocumentVerificationDto>> Handle(VerifyDocumentQuery query, CancellationToken cancellationToken) =>
        await documents.GetByCodeAsync(query.Code, cancellationToken) is { } document
            ? new DocumentVerificationDto(true, document.Kind, document.Snapshot.Number, document.IssuedAt, document.Snapshot.Total, document.Snapshot.ShopName)
            : new DocumentVerificationDto(false, null, null, null, null, null);
}
