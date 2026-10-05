using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Auth;
using Oficina.Api.ExceptionHandling;
using Oficina.Api.Extensions;
using Oficina.Application.Identity.Commands.Login;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Queries.GetCurrentUser;

namespace Oficina.Api.Controllers.Auth;

[Route("api/auth")]
[Tags("Auth")]
public sealed class AuthController(ISender sender) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(SecurityExtensions.LoginRateLimitPolicy)]
    [EndpointSummary("Sign in")]
    [EndpointDescription("Validates e-mail and password and returns a JWT access token. Inactive or deleted users cannot sign in. Limited to 5 attempts per minute per client IP.")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, ProblemResponses.ContentType)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken), Ok);

    [HttpGet("me")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Current user")]
    [EndpointDescription("Returns id, name, e-mail and role of the signed-in user.")]
    [ProducesResponseType<CurrentUserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetCurrentUserQuery(), cancellationToken), Ok);
}
