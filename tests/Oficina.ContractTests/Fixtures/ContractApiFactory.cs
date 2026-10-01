using System.Net.Http.Headers;
using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Infrastructure.Messaging.Outbox;

namespace Oficina.ContractTests.Fixtures;

public sealed class ContractApiFactory : WebApplicationFactory<Program>
{
    public ISender Sender { get; } = Substitute.For<ISender>();

    public HttpClient CreateClient(Role role)
    {
        var user = User.Create("Contract User", Email.Create("contract@oficina.com").Value, "$2a$12$hash", role).Value;
        var token = Services.GetRequiredService<ITokenGenerator>().Generate(user);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Oficina", "Server=contract-tests;Database=none;Integrated Security=false");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Jwt:Secret", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        builder.ConfigureServices(services =>
        {
            services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
            var outboxProcessor = services.Single(descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(OutboxProcessor));
            services.Remove(outboxProcessor);
            services.AddSingleton(Sender);
        });
    }
}
