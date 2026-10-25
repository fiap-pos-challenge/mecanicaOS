using MediatR;
using Oficina.Application.Documents.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Documents.Repositories;
using Oficina.Domain.Portal.Errors;

namespace Oficina.Application.Documents.Queries.GetDocument;

public sealed class GetDocumentQueryHandler(IDocumentRepository documents) : IRequestHandler<GetDocumentQuery, Result<DocumentDto>>
{
    public async Task<Result<DocumentDto>> Handle(GetDocumentQuery query, CancellationToken cancellationToken)
    {
        if (await documents.GetByCodeAsync(query.Code, cancellationToken) is not { } document || (query.ServiceOrderId is { } orderId && document.ServiceOrderId != orderId))
        {
            return PortalErrors.DocumentNotFound;
        }

        return document.ToDto();
    }
}
