using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Operations;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Notifications.Commands.MarkAllNotificationsRead;
using Oficina.Application.Notifications.Commands.MarkNotificationRead;
using Oficina.Application.Notifications.Commands.SaveNotificationPreferences;
using Oficina.Application.Notifications.Dtos;
using Oficina.Application.Notifications.Queries.GetNotificationPreferences;
using Oficina.Application.Notifications.Queries.ListNotifications;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/notifications")]
[Tags("Notifications")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class NotificationsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List notifications")]
    [EndpointDescription("Returns the 50 latest team notifications with the read flag of the signed-in user, hiding the types the user turned off in the app.")]
    [ProducesResponseType<NotificationListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListNotificationsQuery(), cancellationToken), Ok);

    [HttpPost("read-all")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Mark all as read")]
    [EndpointDescription("Marks the latest notifications as read for the signed-in user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReadAll(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken));

    [HttpPost("{id:guid}/read")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Mark as read")]
    [EndpointDescription("Marks one notification as read for the signed-in user.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new MarkNotificationReadCommand(id), cancellationToken));

    [HttpGet("preferences")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get notification preferences")]
    [EndpointDescription("Returns, per notification type, whether the signed-in user wants it in the app, by e-mail and by WhatsApp.")]
    [ProducesResponseType<NotificationPreferenceListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Preferences(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetNotificationPreferencesQuery(), cancellationToken), Ok);

    [HttpPut("preferences")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Save notification preferences")]
    [EndpointDescription("Saves the channels of each notification type for the signed-in user. E-mail copies go to the user's address.")]
    [ProducesResponseType<NotificationPreferenceListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> SavePreferences(NotificationPreferencesBody request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SaveNotificationPreferencesCommand(request.Items), cancellationToken), Ok);
}
