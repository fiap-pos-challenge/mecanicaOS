using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Automations;
using Oficina.Application.Notifications;

namespace Oficina.Api.Controllers.Admin;

public sealed record NotificationPreferencesBody(IReadOnlyList<NotificationPreferenceDto>? Items);

public sealed record AutomationRuleBody(RuleDefinitionDto? Definition);

public sealed record AutomationStatusBody(bool? Enabled);

[Route("api/admin/notifications")]
[Tags("Notifications")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class NotificationsController(NotificationService notifications) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List notifications")]
    [EndpointDescription("Returns the 50 latest team notifications with the read flag of the signed-in user, hiding the types the user turned off in the app.")]
    [ProducesResponseType<NotificationListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Respond(await notifications.ListAsync(cancellationToken), Ok);

    [HttpPost("read-all")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Mark all as read")]
    [EndpointDescription("Marks the latest notifications as read for the signed-in user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReadAll(CancellationToken cancellationToken) =>
        Respond(await notifications.ReadAllAsync(cancellationToken));

    [HttpPost("{id:guid}/read")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Mark as read")]
    [EndpointDescription("Marks one notification as read for the signed-in user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken) =>
        Respond(await notifications.ReadAsync(id, cancellationToken));

    [HttpGet("preferences")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get notification preferences")]
    [EndpointDescription("Returns, per notification type, whether the signed-in user wants it in the app, by e-mail and by WhatsApp.")]
    [ProducesResponseType<NotificationPreferenceListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Preferences(CancellationToken cancellationToken) =>
        Respond(await notifications.PreferencesAsync(cancellationToken), Ok);

    [HttpPut("preferences")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Save notification preferences")]
    [EndpointDescription("Saves the channels of each notification type for the signed-in user. E-mail copies go to the user's address.")]
    [ProducesResponseType<NotificationPreferenceListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> SavePreferences(NotificationPreferencesBody request, CancellationToken cancellationToken) =>
        Respond(await notifications.SavePreferencesAsync(request.Items, cancellationToken), Ok);
}

[Route("api/admin/automations")]
[Tags("Automations")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class AutomationsController(AutomationService automations) : ApiControllerBase
{
    [HttpGet("rules")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("List automation rules")]
    [EndpointDescription("Returns the rules with trigger, conditions, wait, actions, runs in the last 24 hours and last run. Roles: Admin, Manager.")]
    [ProducesResponseType<AutomationRuleListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Rules(CancellationToken cancellationToken) =>
        Respond(await automations.ListAsync(cancellationToken), Ok);

    [HttpPost("rules")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Create automation rule")]
    [EndpointDescription("Creates an enabled rule. 422 automations.invalid explains what to fix. Roles: Admin, Manager.")]
    [ProducesResponseType<AutomationRuleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(AutomationRuleBody request, CancellationToken cancellationToken) =>
        Respond(await automations.CreateAsync(request.Definition, cancellationToken), rule => StatusCode(StatusCodes.Status201Created, rule));

    [HttpPut("rules/{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Update automation rule")]
    [EndpointDescription("Replaces the definition of a rule. Roles: Admin, Manager.")]
    [ProducesResponseType<AutomationRuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, AutomationRuleBody request, CancellationToken cancellationToken) =>
        Respond(await automations.UpdateAsync(id, request.Definition, cancellationToken), Ok);

    [HttpPut("rules/{id:guid}/status")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Turn rule on or off")]
    [EndpointDescription("Enables or disables a rule. Disabled rules skip pending runs. Roles: Admin, Manager.")]
    [ProducesResponseType<AutomationRuleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Status(Guid id, AutomationStatusBody request, CancellationToken cancellationToken) =>
        Respond(await automations.SetStatusAsync(id, request.Enabled, cancellationToken), Ok);

    [HttpDelete("rules/{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Delete automation rule")]
    [EndpointDescription("Deletes a rule and its run history. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        Respond(await automations.DeleteAsync(id, cancellationToken));

    [HttpGet("queues")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Queue health")]
    [EndpointDescription("Returns processed (last 24 hours), failed and pending items of each background queue. Roles: Admin, Manager.")]
    [ProducesResponseType<QueueHealthListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Queues(CancellationToken cancellationToken) =>
        Respond(await automations.QueuesAsync(cancellationToken), Ok);

    [HttpGet("queues/failures")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Queue failures")]
    [EndpointDescription("Returns the failed automation runs and event deliveries with the error and attempts. Roles: Admin, Manager.")]
    [ProducesResponseType<QueueFailureListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Failures(CancellationToken cancellationToken) =>
        Respond(await automations.FailuresAsync(cancellationToken), Ok);

    [HttpPost("queues/failures/{id:guid}/retry")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Retry failure")]
    [EndpointDescription("Puts a failed item back in its queue. Roles: Admin.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var result = await automations.RetryAsync(id, cancellationToken);
        return result.Error is { } error ? ProblemFor(error) : Accepted();
    }
}
