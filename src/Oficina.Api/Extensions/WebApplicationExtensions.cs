using Oficina.Api.Configuration;

namespace Oficina.Api.Extensions;

public static class WebApplicationExtensions
{
    public const string HealthRoute = "/health";

    public static WebApplication UsePresentation(this WebApplication app)
    {
        app.UseExceptionHandler();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(CorsSettings.PolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks(HealthRoute).AllowAnonymous();

        return app;
    }
}
