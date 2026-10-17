using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Abstractions.Addresses;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Identity.Entities;
using Oficina.Infrastructure.Addresses;

namespace Oficina.FunctionalTests.Fixtures;

public sealed class FunctionalApiFactory(SqlServerFixture database) : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:4200";
    public const string PixWebhookSecret = "segredo-pix-teste";

    public CapturingEmailSender Emails { get; } = new();

    public StubHttpHandler ZipCodes { get; } = new(request => request.RequestUri!.AbsolutePath.Contains("01310100", StringComparison.Ordinal)
        ? StubHttpHandler.Json("""{"cep":"01310-100","logradouro":"Avenida Paulista","bairro":"Bela Vista","localidade":"São Paulo","uf":"SP"}""")
        : StubHttpHandler.Json("""{"erro":true}"""));

    public HttpClient CreateClient(User user)
    {
        var token = Services.GetRequiredService<ITokenGenerator>().Generate(user);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Oficina", database.ConnectionString);
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Outbox:PollingIntervalSeconds", "1");
        builder.UseSetting("Jwt:Secret", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.UseSetting("DevelopmentAdmin:Email", string.Empty);
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
        builder.UseSetting("Payments:PixWebhookSecret", PixWebhookSecret);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEmailSender>(Emails);
            services.AddHttpClient<IAddressLookup, ViaCepAddressLookup>(client => client.BaseAddress = new Uri("https://viacep.test/"))
                .ConfigurePrimaryHttpMessageHandler(() => ZipCodes);
        });
    }
}
