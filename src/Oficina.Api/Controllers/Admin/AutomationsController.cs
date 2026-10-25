using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Operations;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Automations.Commands.CreateAutomationRule;
using Oficina.Application.Automations.Commands.DeleteAutomationRule;
using Oficina.Application.Automations.Commands.RetryQueueFailure;
using Oficina.Application.Automations.Commands.SetAutomationRuleStatus;
using Oficina.Application.Automations.Commands.UpdateAutomationRule;
using Oficina.Application.Automations.Dtos;
using Oficina.Application.Automations.Queries.ListAutomationRules;
using Oficina.Application.Automations.Queries.ListQueueFailures;
using Oficina.Application.Automations.Queries.ListQueueHealth;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/automations")]
[Tags("Automations")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class AutomationsController(ISender sender) : ApiControllerBase
{
    [HttpGet("rules")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("List automation rules")]
    [EndpointDescription("Returns the rules with trigger, conditions, wait, actions, runs in the last 24 hours and last run. Roles: Admin, Manager.")]
    [ProducesResponseType<AutomationRuleListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Rules(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListAutomationRulesQuery(), cancellationToken), Ok);

    [HttpPost("rules")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Create automation rule")]
    [EndpointDescription("Creates an enabled rule. 422 automations.invalid explains what to fix. Roles: Admin, Manager.")]
    [ProducesResponseType<AutomationRuleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(AutomationRuleBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CreateAutomationRuleCommand(request.Definition), cancellationToken), rule => StatusCode(StatusCodes.Status201Created, rule));

    [HttpPut("rules/{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Update automation rule")]
    [EndpointDescription("Replaces the definition of a rule. Roles: Admin, Manager.")]
    [ProducesResponseType<AutomationRuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, AutomationRuleBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new UpdateAutomationRuleCommand(id, request.Definition), cancellationToken), Ok);

    [HttpPut("rules/{id:guid}/status")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Turn rule on or off")]
    [EndpointDescription("Enables or disables a rule. Disabled rules skip pending runs. Roles: Admin, Manager.")]
    [ProducesResponseType<AutomationRuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Status(Guid id, AutomationStatusBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SetAutomationRuleStatusCommand(id, request.Enabled), cancellationToken), Ok);

    [HttpDelete("rules/{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Delete automation rule")]
    [EndpointDescription("Deletes a rule and its run history. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeleteAutomationRuleCommand(id), cancellationToken));

    [HttpGet("queues")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Queue health")]
    [EndpointDescription("Returns processed (last 24 hours), failed and pending items of each background queue. Roles: Admin, Manager.")]
    [ProducesResponseType<QueueHealthListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Queues(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListQueueHealthQuery(), cancellationToken), Ok);

    [HttpGet("queues/failures")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Queue failures")]
    [EndpointDescription("Returns the failed automation runs and event deliveries with the error and attempts. Roles: Admin, Manager.")]
    [ProducesResponseType<QueueFailureListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Failures(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListQueueFailuresQuery(), cancellationToken), Ok);

    [HttpPost("queues/failures/{id:guid}/retry")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Retry failure")]
    [EndpointDescription("Puts a failed item back in its queue. Roles: Admin.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RetryQueueFailureCommand(id), cancellationToken);
        return result.Error is { } error ? ProblemFor(error) : Accepted();
    }
}
