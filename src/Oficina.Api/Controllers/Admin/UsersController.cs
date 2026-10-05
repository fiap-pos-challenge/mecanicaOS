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
using Oficina.Application.Users.Commands.ReactivateUser;
using Oficina.Application.Users.Commands.UpdateUser;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Queries.GetUserById;
using Oficina.Application.Users.Queries.ListUsers;
using Oficina.Application.Users.Queries.SearchUserByEmail;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/users")]
[Tags("Users")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class UsersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("List users")]
    [EndpointDescription("Returns a page of users ordered by name. Role: Admin.")]
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListUsersQuery(page, pageSize), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Create user")]
    [EndpointDescription("Creates a user with any role (Admin, Attendant or Mechanic), unique e-mail and strong password. Role: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Name, request.Email, request.Password, request.Role);
        return Respond(await sender.Send(command, cancellationToken), user => CreatedAtAction(nameof(GetById), new { id = user.Id }, user));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Get user")]
    [EndpointDescription("Returns one user by id. Role: Admin.")]
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
