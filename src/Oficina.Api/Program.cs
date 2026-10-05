using Oficina.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddStructuredLogging();

builder.Services
    .AddDomain()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSecurity(builder.Configuration)
    .AddPresentation();

var app = builder.Build();

await app.ApplyDatabaseMigrationsAsync();
app.UsePresentation();

await app.RunAsync();
