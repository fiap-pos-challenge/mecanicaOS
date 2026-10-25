using MediatR;
using Oficina.Application.Abstractions.Links;
using Oficina.Application.Documents.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Documents.Repositories;
using Oficina.Domain.Portal.Errors;

namespace Oficina.Application.Documents.Queries.GetDocumentPdf;

public sealed class GetDocumentPdfQueryHandler(IDocumentRepository documents, IPublicLinks links) : IRequestHandler<GetDocumentPdfQuery, Result<DocumentDownloadDto>>
{
    public async Task<Result<DocumentDownloadDto>> Handle(GetDocumentPdfQuery query, CancellationToken cancellationToken)
    {
        if (await documents.GetByCodeAsync(query.Code, cancellationToken) is not { } found || (query.ServiceOrderId is { } orderId && found.ServiceOrderId != orderId))
        {
            return PortalErrors.DocumentNotFound;
        }

        var document = found.ToDto();
        var bytes = PdfWriter.Write(DocumentTexts.PdfLines(document, links.DocumentVerification(document.Code)));
        return new DocumentDownloadDto($"data:application/pdf;base64,{Convert.ToBase64String(bytes)}", $"{document.Code}.pdf");
    }
}
