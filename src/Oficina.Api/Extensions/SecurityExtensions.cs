using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Oficina.Api.Authorization;
using Oficina.Api.Configuration;
using Oficina.Api.ExceptionHandling;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Identity.Enums;
using Oficina.Infrastructure.Security;

namespace Oficina.Api.Extensions;

public static class SecurityExtensions
{
    public const string LoginRateLimitPolicy = "login";
    public const string InvitationRateLimitPolicy = "invitation";
    public const int LoginPermitLimit = 5;
    public const string LoginPermitLimitKey = "Security:LoginRateLimit:PermitLimit";

    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Value.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Secret ?? string.Empty)),
                    ClockSkew = ClockSkew,
                    RoleClaimType = JwtTokenGenerator.RoleClaimType,
                    NameClaimType = "sub",
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Authenticated, policy => policy.RequireAuthenticatedUser())
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireAuthenticatedUser().RequireRole(nameof(Role.Admin)))
            .AddPolicy(Policies.AdminOrManager, policy => policy.RequireAuthenticatedUser().RequireRole(nameof(Role.Admin), nameof(Role.Manager)))
            .AddPolicy(Policies.Staff, policy => policy.RequireAuthenticatedUser().RequireRole(nameof(Role.Admin), nameof(Role.Manager), nameof(Role.Attendant)));
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();

        var cors = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>();
        services.AddCors(options => options.AddPolicy(CorsSettings.PolicyName, policy => policy
            .WithOrigins(cors?.AllowedOrigins ?? Array.Empty<string>())
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
            .WithHeaders("Authorization", "Content-Type")));

        var loginPermitLimit = configuration.GetValue(LoginPermitLimitKey, LoginPermitLimit);
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(LoginRateLimitPolicy, context => PerClientIp(context, loginPermitLimit));
            options.AddPolicy(InvitationRateLimitPolicy, context => PerClientIp(context, loginPermitLimit));
            options.OnRejected = async (context, _) =>
            {
                var error = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? CommonErrors.TooManyRequests with { RetryAfter = retryAfter }
                    : CommonErrors.TooManyRequests;
                await ProblemResponses.WriteAsync(context.HttpContext, error, StatusCodes.Status429TooManyRequests);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> PerClientIp(HttpContext context, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = LoginWindow, QueueLimit = 0 });
}
