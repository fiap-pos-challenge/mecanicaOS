using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Jobs;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Jobs.Commands.ExportCustomerData;
using Oficina.Application.Jobs.Commands.ImportCustomers;
using Oficina.Application.Jobs.Dtos;
using Oficina.Application.Jobs.Queries.GetCustomerSummaries;
using Oficina.Application.Jobs.Queries.GetJob;
using Oficina.Application.Jobs.Queries.GetServiceHistory;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin")]
[Tags("Customer insights")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class JobsController(ISender sender) : ApiControllerBase
{
    [HttpPost("customers/summaries")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Customer summaries")]
    [EndpointDescription("Returns vehicles, last visit, total spent, average ticket, open orders and situation (Active, OpenServiceOrder, NoReturn after 90 days) for up to 500 customers sent in the body. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<CustomerSummaryListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Summaries(CustomerSummariesBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetCustomerSummariesQuery(request.CustomerIds), cancellationToken), Ok);

    [HttpGet("customers/{id:guid}/service-history")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Customer service history")]
    [EndpointDescription("Returns the service orders of the customer from the newest with description, total, status and payment method. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<ServiceHistoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> History(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetServiceHistoryQuery(id), cancellationToken), Ok);

    [HttpPost("customers/{id:guid}/data-export")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Export customer data")]
    [EndpointDescription("Starts the LGPD export of every personal data kept about the customer (record, vehicles, orders, access log). Poll GET /api/admin/jobs/{id} for the JSON file. Roles: Admin, Manager.")]
    [ProducesResponseType<JobDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Export(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ExportCustomerDataCommand(id), cancellationToken), job => StatusCode(StatusCodes.Status202Accepted, job));

    [HttpPost("imports/customers")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Import customers")]
    [EndpointDescription("Imports customers from a CSV (nome;documento;telefone;email, up to 2 MB). Valid lines become customers; the job reports the errors by line. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<JobDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, ProblemResponses.ContentType)]
    public async Task<IActionResult> Import(ImportCustomersBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ImportCustomersCommand(request.FileName, request.Csv), cancellationToken), job => StatusCode(StatusCodes.Status202Accepted, job));

    [HttpGet("jobs/{id:guid}")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Get job")]
    [EndpointDescription("Returns the status and result of an export or import job. Exports are visible only to Admin and Manager. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<JobDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetJobQuery(id), cancellationToken), Ok);
}
