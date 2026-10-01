using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Customers.Commands.CreateCustomer;
using Oficina.Application.Customers.Commands.DeleteCustomer;
using Oficina.Application.Customers.Queries.SearchCustomerByDocument;
using Oficina.Application.Identity.Commands.Login;
using Oficina.Application.Users.Commands.DeactivateUser;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;
using Oficina.FunctionalTests.Fixtures;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence;

namespace Oficina.FunctionalTests.Application;

[Collection(nameof(DatabaseCollection))]
public sealed class ApplicationPipelineTests(SqlServerFixture database) : IAsyncLifetime
{
    private const string AdminPassword = "Oficina@2026";
    private FunctionalApiFactory? _factory;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private FunctionalApiFactory Factory => _factory ?? throw new InvalidOperationException(nameof(Factory));

    public async ValueTask InitializeAsync()
    {
        await database.ResetAsync();
        _factory = new FunctionalApiFactory(database);
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task InvalidCommand_IsRejectedByTheValidationBehaviorWithCamelCaseFields()
    {
        var result = await SendAsync(new CreateCustomerCommand("Maria", "123", null, "11987654321", null));

        Assert.Equal(CommonErrors.ValidationFailed.Code, result.Error!.Code);
        var field = Assert.Single(result.Error.Fields);
        Assert.Equal("document", field.Field);
        Assert.Equal(CustomerErrors.InvalidDocument.Code, field.Code);
    }

    [Fact]
    public async Task CreatedCustomer_IsFoundBySearchingTheDocumentInTheBody()
    {
        var created = await SendAsync(new CreateCustomerCommand("Maria Souza", "529.982.247-25", null, "11987654321", null));

        var found = await SendAsync(new SearchCustomerByDocumentQuery("52998224725"));

        Assert.Equal(created.Value.Id, found.Value.Id);
        Assert.Equal(CustomerErrors.DuplicateDocument, (await SendAsync(new CreateCustomerCommand("Outra", "52998224725", null, "11987654321", null))).Error);
    }

    [Fact]
    public async Task DeleteCustomer_WithActiveVehicle_IsBlocked()
    {
        var customer = TestData.Customer();
        await using (var context = database.CreateContext(TimeProvider.System))
        {
            context.Customers.Add(customer);
            context.Vehicles.Add(TestData.Vehicle(customer.Id));
            Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(CancellationToken)).IsSuccess);
        }

        var result = await SendAsync(new DeleteCustomerCommand(customer.Id));

        Assert.Equal(CustomerErrors.HasActiveVehicles, result.Error);
        Assert.Equal(1, await database.CountRowsAsync("customers", customer.Id));
    }

    [Fact]
    public async Task Admin_CanLogInOnlyWithTheRightPassword()
    {
        await SaveAdminAsync();

        var login = await SendAsync(new LoginCommand("admin@oficina.com", AdminPassword));
        var wrongPassword = await SendAsync(new LoginCommand("admin@oficina.com", "Errada@2026"));

        Assert.Equal(3, login.Value.AccessToken.Split('.').Length);
        Assert.Equal(IdentityErrors.InvalidCredentials, wrongPassword.Error);
    }

    [Fact]
    public async Task Login_UnknownEmail_TakesAboutAsLongAsAWrongPassword()
    {
        await SaveAdminAsync();
        await SendAsync(new LoginCommand("aquecimento@oficina.com", AdminPassword));

        var wrongPassword = await MeasureAsync(new LoginCommand("admin@oficina.com", "Errada@2026"));
        var unknownEmail = await MeasureAsync(new LoginCommand("desconhecido@oficina.com", "Errada@2026"));

        Assert.True(unknownEmail >= wrongPassword / 2, $"unknown={unknownEmail.TotalMilliseconds}ms wrong={wrongPassword.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task InactiveUser_CannotLogIn()
    {
        await SaveAdminAsync();
        await database.QueryAsync("UPDATE users SET isActive = 0; SELECT 1 AS done");

        var login = await SendAsync(new LoginCommand("admin@oficina.com", AdminPassword));

        Assert.Equal(IdentityErrors.InvalidCredentials, login.Error);
    }

    [Fact]
    public async Task AdminCommand_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        var result = await SendAsync(new DeactivateUserCommand(Guid.CreateVersion7()));

        Assert.Equal(CommonErrors.Unauthorized, result.Error);
    }

    private async Task SaveAdminAsync()
    {
        var hash = Factory.Services.GetRequiredService<IPasswordHasher>().Hash(AdminPassword);
        await using var context = database.CreateContext(TimeProvider.System);
        context.Users.Add(User.Create("Administrador", Email.Create("admin@oficina.com").Value, hash, Role.Admin).Value);
        Assert.True((await new UnitOfWork(context, new OutboxWriter()).CommitAsync(CancellationToken)).IsSuccess);
    }

    private async Task<TimeSpan> MeasureAsync(LoginCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await SendAsync(command);
        stopwatch.Stop();
        Assert.Equal(IdentityErrors.InvalidCredentials, result.Error);
        return stopwatch.Elapsed;
    }

    private async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request, CancellationToken);
    }
}
