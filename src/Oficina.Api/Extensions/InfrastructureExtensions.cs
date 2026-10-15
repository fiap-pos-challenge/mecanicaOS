using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Customers.Queries;
using Oficina.Application.Users.Queries;
using Oficina.Application.Vehicles.Queries;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Notifications;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Persistence.Interceptors;
using Oficina.Infrastructure.Persistence.Seeding;
using Oficina.Infrastructure.Queries.Catalog;
using Oficina.Infrastructure.Queries.Customers;
using Oficina.Infrastructure.Queries.Identity;
using Oficina.Infrastructure.Queries.Vehicles;
using Oficina.Infrastructure.Repositories.Catalog;
using Oficina.Infrastructure.Repositories.Customers;
using Oficina.Infrastructure.Repositories.Identity;
using Oficina.Infrastructure.Repositories.Inventory;
using Oficina.Infrastructure.Repositories.ServiceOrders;
using Oficina.Infrastructure.Security;

namespace Oficina.Api.Extensions;

public static class InfrastructureExtensions
{
    private const string ConnectionStringName = "Oficina";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Configure(options => options.ConnectionString = configuration.GetConnectionString(ConnectionStringName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<AuditingInterceptor>();
        services.AddDbContext<OficinaDbContext>((provider, options) => options
            .UseSqlServer(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString)
            .AddInterceptors(provider.GetRequiredService<AuditingInterceptor>()));

        services.AddScoped<OutboxWriter>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IPartRepository, PartRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<ICustomerQueries, CustomerQueries>();
        services.AddScoped<IServiceQueries, ServiceQueries>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IVehicleQueries, VehicleQueries>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddOptions<DevelopmentAdminOptions>().Bind(configuration.GetSection(DevelopmentAdminOptions.SectionName));
        services.AddScoped<DevelopmentAdminSeeder>();

        services.AddScoped<INotifier, LogNotifier>();
        services.AddScoped<OutboxBatchProcessor>();
        services.AddHostedService<OutboxProcessor>();

        services.AddHealthChecks().AddDbContextCheck<OficinaDbContext>();

        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
        await context.Database.MigrateAsync();

        if (app.Environment.IsDevelopment())
        {
            await scope.ServiceProvider.GetRequiredService<DevelopmentAdminSeeder>().SeedAsync(CancellationToken.None);
        }
    }
}
