using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Auth;
using Oficina.Api.ExceptionHandling;
using Oficina.Api.Extensions;
using Oficina.Application.Identity.Commands.AcceptInvitation;
using Oficina.Application.Identity.Commands.Login;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Queries.GetCurrentUser;
using Oficina.Application.Identity.Queries.InspectInvitation;

namespace Oficina.Api.Controllers.Auth;

[Route("api/auth")]
[Tags("Auth")]
public sealed class AuthController(ISender sender) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.LoginRateLimitPolicy)]
    [EndpointSummary("Sign in")]
    [EndpointDescription("Validates e-mail and password and returns a JWT access token. Wrong password, unknown e-mail, deleted users and invited users who have not defined a password yet get the same 401. An inactive user with the right password gets 403 identity.accountDisabled. Five wrong passwords in a row lock the account for 15 minutes (429 identity.accountLocked with Retry-After). Limited to 5 attempts per minute per client IP (429 common.tooManyRequests with Retry-After).")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemResponses.ContentType)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken), Ok);

    [HttpPost("invitations/inspect")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.InvitationRateLimitPolicy)]
    [EndpointSummary("Inspect invitation")]
    [EndpointDescription("Returns the invited name, masked e-mail, role and expiry for the token sent in the body (never in the URL). Unknown token: 404 identity.invitationNotFound; expired: 422 identity.invitationExpired; used or replaced: 409 identity.invitationAlreadyUsed. Limited to 5 attempts per minute per client IP, counted apart from sign in.")]
    [ProducesResponseType<InvitationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemResponses.ContentType)]
    public async Task<IActionResult> InspectInvitation(InspectInvitationRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new InspectInvitationQuery(request.Token), cancellationToken), Ok);

    [HttpPost("invitations/accept")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.InvitationRateLimitPolicy)]
    [EndpointSummary("Accept invitation")]
    [EndpointDescription("Defines the password of an invited user with a strong password and consumes the single-use token. Same errors as inspect, plus 400 identity.weakPassword. Limited to 5 attempts per minute per client IP, counted apart from sign in.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemResponses.ContentType)]
    public async Task<IActionResult> AcceptInvitation(AcceptInvitationRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new AcceptInvitationCommand(request.Token, request.Password), cancellationToken));

    [HttpGet("me")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Current user")]
    [EndpointDescription("Returns id, name, e-mail and role of the signed-in user.")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetCurrentUserQuery(), cancellationToken), Ok);
}
