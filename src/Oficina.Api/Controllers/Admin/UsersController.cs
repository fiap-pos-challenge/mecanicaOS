using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Users;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Commands.CreateUser;
using Oficina.Application.Users.Commands.DeactivateUser;
using Oficina.Application.Users.Commands.DeleteUser;
using Oficina.Application.Users.Commands.InviteUser;
using Oficina.Application.Users.Commands.ReactivateUser;
using Oficina.Application.Users.Commands.ResendInvitation;
using Oficina.Application.Users.Commands.UpdateUser;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Queries.GetUserById;
using Oficina.Application.Users.Queries.ListUsers;
using Oficina.Application.Users.Queries.SearchUserByEmail;
using Oficina.Domain.Identity.Enums;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/users")]
[Tags("Users")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class UsersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("List users")]
    [EndpointDescription("Returns a page of users ordered by name, optionally filtered by role and status (Active, Inactive or InvitationPending). Roles: Admin, Manager.")]
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] Role? role = null,
        [FromQuery] UserStatus? status = null,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListUsersQuery(page, pageSize, role, status), cancellationToken), Ok);

    [HttpPost("invite")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Invite user")]
    [EndpointDescription("Creates a user without password and e-mails a single-use link (valid for 72 hours) where the person defines their own password. Nobody, not even the Admin, sees the password. Role: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Invite(InviteUserRequest request, CancellationToken cancellationToken) =>
        Respond(
            await sender.Send(new InviteUserCommand(request.Name, request.Email, request.Role), cancellationToken),
            user => CreatedAtAction(nameof(GetById), new { id = user.Id }, user));

    [HttpPost("{id:guid}/invite/resend")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Resend invitation")]
    [EndpointDescription("Revokes the open invitation and e-mails a new link. Only for users who have not defined a password yet. Role: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ResendInvitationCommand(id), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Create user")]
    [EndpointDescription("Creates a user with any role (Admin, Manager, Attendant or Mechanic), unique e-mail and strong password. Role: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Name, request.Email, request.Password, request.Role);
        return Respond(await sender.Send(command, cancellationToken), user => CreatedAtAction(nameof(GetById), new { id = user.Id }, user));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Get user")]
    [EndpointDescription("Returns one user by id. Roles: Admin, Manager.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetUserByIdQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Update user")]
    [EndpointDescription("Changes name and role. An Admin cannot demote themselves, and the last active Admin cannot be demoted. Role: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new UpdateUserCommand(id, request.Name, request.Role), cancellationToken), Ok);

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Delete user")]
    [EndpointDescription("Deactivates and soft deletes a user. An Admin cannot delete themselves or the last active Admin. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeleteUserCommand(id), cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Deactivate user")]
    [EndpointDescription("Blocks the user from signing in. An Admin cannot deactivate themselves or the last active Admin. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivateUserCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Reactivate user")]
    [EndpointDescription("Allows an inactive user to sign in again. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivateUserCommand(id), cancellationToken));

    [HttpPost("search")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Search user by e-mail")]
    [EndpointDescription("Finds a user by e-mail sent in the body (never in the URL). Role: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Search(SearchUserRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SearchUserByEmailQuery(request.Email), cancellationToken), Ok);
}
