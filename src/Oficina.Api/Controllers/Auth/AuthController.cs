using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Auth;
using Oficina.Api.ExceptionHandling;
using Oficina.Api.Extensions;
using Oficina.Application.Identity.Commands.AcceptInvitation;
using Oficina.Application.Identity.Commands.DisableTwoFactor;
using Oficina.Application.Identity.Commands.EnableTwoFactor;
using Oficina.Application.Identity.Commands.Login;
using Oficina.Application.Identity.Commands.Logout;
using Oficina.Application.Identity.Commands.Reauthenticate;
using Oficina.Application.Identity.Commands.RefreshSession;
using Oficina.Application.Identity.Commands.SetupTwoFactor;
using Oficina.Application.Identity.Commands.VerifyTwoFactor;
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
        Respond(await sender.Send(new LoginCommand(request.Email, request.Password, SessionCookies.ReadDevice(Request)), cancellationToken), StartSession);

    [HttpPost("2fa/verify")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.LoginRateLimitPolicy)]
    [EndpointSummary("Verify two-factor code")]
    [EndpointDescription("Completes a sign in that answered requiresTwoFactor with the challenge id and the 6-digit code of the authenticator app. The challenge expires in 5 minutes and allows 5 attempts. Wrong or expired code: 401 identity.invalidTwoFactorCode.")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new VerifyTwoFactorCommand(request.ChallengeId, request.Code, SessionCookies.ReadDevice(Request)), cancellationToken), StartSession);

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EndpointSummary("Refresh session")]
    [EndpointDescription("Issues a new access token from the HttpOnly refresh cookie set at sign in and rotates the cookie. Missing, revoked or expired session: 401 identity.sessionExpired.")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshSessionCommand(SessionCookies.ReadRefresh(Request)), cancellationToken);
        if (result.IsFailure)
        {
            SessionCookies.ClearRefresh(Response);
        }

        return Respond(result, StartSession);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [EndpointSummary("Sign out")]
    [EndpointDescription("Revokes the session of the refresh cookie and deletes the cookie. Always 204.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(SessionCookies.ReadRefresh(Request)), cancellationToken);
        SessionCookies.ClearRefresh(Response);
        return Respond(result);
    }

    [HttpPost("reauthenticate")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Confirm identity")]
    [EndpointDescription("Confirms the signed-in user with the password or, when two-factor is on, the authenticator code, and returns a single-use token valid for 5 minutes. Send it in the X-Reauthentication header of sensitive changes. Wrong proof: 422 auth.reauthenticationFailed.")]
    [ProducesResponseType<ReauthenticationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reauthenticate(ReauthenticateRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReauthenticateCommand(request.Password, request.Code), cancellationToken), Ok);

    [HttpPost("2fa/setup")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Set up two-factor")]
    [EndpointDescription("Generates a new authenticator secret for the signed-in user and returns it with the otpauth URI for the QR code. Two-factor stays off until enabled with a valid code.")]
    [ProducesResponseType<TwoFactorSetupDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SetupTwoFactorCommand(), cancellationToken), Ok);

    [HttpPost("2fa/enable")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Enable two-factor")]
    [EndpointDescription("Turns two-factor on after checking a code of the authenticator app configured with the secret from setup. Without setup: 409 identity.twoFactorNotSetUp; wrong code: 400 identity.invalidTwoFactorCode.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new EnableTwoFactorCommand(request.Code), cancellationToken));

    [HttpPost("2fa/disable")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Disable two-factor")]
    [EndpointDescription("Turns two-factor off and removes the secret. Requires the X-Reauthentication header (403 settings.reauthenticationRequired).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisableTwoFactor([FromHeader(Name = SessionCookies.ReauthenticationHeader)] string? reauthentication, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DisableTwoFactorCommand(reauthentication), cancellationToken));

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

    private OkObjectResult StartSession(LoginResult result)
    {
        if (result.RefreshToken is { } refreshToken && result.RefreshExpiresAt is { } expiresAt)
        {
            SessionCookies.WriteRefresh(Response, refreshToken, expiresAt);
        }

        return Ok(result.Response);
    }
}
