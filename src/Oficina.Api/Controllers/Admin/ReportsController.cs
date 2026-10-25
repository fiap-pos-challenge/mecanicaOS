using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Reports;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Reports.Commands.CreateSavedReport;
using Oficina.Application.Reports.Commands.DeleteSavedReport;
using Oficina.Application.Reports.Commands.ExportReport;
using Oficina.Application.Reports.Commands.ScheduleSavedReport;
using Oficina.Application.Reports.Commands.UpdateSavedReport;
using Oficina.Application.Reports.Dtos;
using Oficina.Application.Reports.Queries.GetReportExport;
using Oficina.Application.Reports.Queries.ListSavedReports;
using Oficina.Application.Reports.Queries.RunReport;
using Oficina.Application.Reports.Queries.SuggestReport;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin")]
[Tags("Reports")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class ReportsController(ISender sender) : ApiControllerBase
{
    [HttpPost("reports/run")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Run report")]
    [EndpointDescription("Runs a report over delivered service orders: measure (revenue, orderCount, partsOut, averageTicket, margin, commission) grouped by month, week, mechanic, service, category, payment method or customer. Money in cents. 422 reports.incompatible for combinations that do not make sense. Roles: Admin, Manager.")]
    [ProducesResponseType<ReportResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Run(RunReportBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RunReportQuery(request.Definition), cancellationToken), Ok);

    [HttpGet("reports/saved")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("List saved reports")]
    [EndpointDescription("Returns the saved reports with definition and schedule, most recently changed first. Roles: Admin, Manager.")]
    [ProducesResponseType<SavedReportListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Saved(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListSavedReportsQuery(), cancellationToken), Ok);

    [HttpPost("reports/saved")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Save report")]
    [EndpointDescription("Saves a report definition with a unique name (3-80 characters). Roles: Admin, Manager.")]
    [ProducesResponseType<SavedReportDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Save(SaveReportBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CreateSavedReportCommand(request.Name, request.Definition), cancellationToken), report => StatusCode(StatusCodes.Status201Created, report));

    [HttpPut("reports/saved/{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Update saved report")]
    [EndpointDescription("Renames a saved report and replaces its definition. Roles: Admin, Manager.")]
    [ProducesResponseType<SavedReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, SaveReportBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new UpdateSavedReportCommand(id, request.Name, request.Definition), cancellationToken), Ok);

    [HttpDelete("reports/saved/{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Delete saved report")]
    [EndpointDescription("Deletes a saved report and its schedule. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeleteSavedReportCommand(id), cancellationToken));

    [HttpPut("reports/saved/{id:guid}/schedule")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Schedule report")]
    [EndpointDescription("Sets the weekly e-mail of a saved report (weekday, HH:mm Brazil time, up to 10 recipients, xlsx or pdf) or removes it with null. Roles: Admin, Manager.")]
    [ProducesResponseType<SavedReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Schedule(Guid id, ScheduleReportBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ScheduleSavedReportCommand(id, request.Schedule), cancellationToken), Ok);

    [HttpPost("reports/exports")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Export report")]
    [EndpointDescription("Exports a report to Excel (xlsx) or PDF. The job returns the file as a data URL. Roles: Admin, Manager.")]
    [ProducesResponseType<ExportJobDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Export(ExportReportBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ExportReportCommand(request.Definition, request.Format), cancellationToken), job => StatusCode(StatusCodes.Status202Accepted, job));

    [HttpGet("reports/exports/{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Get report export")]
    [EndpointDescription("Returns the status and the file of a report export. Roles: Admin, Manager.")]
    [ProducesResponseType<ExportJobDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetExport(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetReportExportQuery(id), cancellationToken), Ok);

    [HttpPost("assistant/report-definitions")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Suggest report")]
    [EndpointDescription("Turns a question in Portuguese (8-300 characters) into a report definition with a short explanation. Runs locally, no external AI. 422 assistant.notUnderstood when no measure is recognized. Roles: Admin, Manager.")]
    [ProducesResponseType<ReportSuggestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Suggest(AssistantQuestionBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SuggestReportQuery(request.Question), cancellationToken), Ok);
}
