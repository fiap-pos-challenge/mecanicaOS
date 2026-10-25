using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Oficina.Api.ExceptionHandling;
using Oficina.Domain.Common.Errors;

namespace Oficina.Api.Authorization;

public sealed class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await context.ChallengeAsync();
            await ProblemResponses.WriteAsync(context, CommonErrors.Unauthorized, StatusCodes.Status401Unauthorized);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await ProblemResponses.WriteAsync(context, CommonErrors.Forbidden, StatusCodes.Status403Forbidden);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
