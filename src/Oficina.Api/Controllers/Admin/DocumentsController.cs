using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Portal;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Documents.Commands.SendDocument;
using Oficina.Application.Documents.Dtos;
using Oficina.Application.Documents.Queries.GetDocument;
using Oficina.Application.Documents.Queries.GetDocumentPdf;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/documents")]
[Tags("Documents")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class DocumentsController(ISender sender) : ApiControllerBase
{
    [HttpGet("{code}")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Get document")]
    [EndpointDescription("Returns the frozen snapshot of an issued document (service order, quote or receipt). Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<DocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Get(string code, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetDocumentQuery(code, null), cancellationToken), Ok);

    [HttpGet("{code}/pdf")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Document PDF")]
    [EndpointDescription("Returns the PDF of an issued document as a data URL with the file name. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<DocumentDownloadDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Pdf(string code, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetDocumentPdfQuery(code, null), cancellationToken), Ok);

    [HttpPost("{code}/send")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Send document")]
    [EndpointDescription("Sends the document to the customer by e-mail (when consented) or queues it for WhatsApp. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<DocumentSendDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Send(string code, SendDocumentBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SendDocumentCommand(code, request.Channel), cancellationToken), Ok);
}
