using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Addresses;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Team.Queries;
using Oficina.Domain.Team.Repositories;
using Oficina.Infrastructure.Queries.Team;
using Oficina.Infrastructure.Repositories.Team;
using Oficina.Application.Catalog.Queries;
using Oficina.Application.Customers.Queries;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Application.Users.Queries;
using Oficina.Application.Vehicles.Queries;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Privacy.Repositories;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Infrastructure.Addresses;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Notifications;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Persistence.Interceptors;
using Oficina.Infrastructure.Persistence.Seeding;
using Oficina.Infrastructure.Queries.Catalog;
using Oficina.Infrastructure.Queries.Customers;
using Oficina.Infrastructure.Queries.Identity;
using Oficina.Infrastructure.Queries.ServiceOrders;
using Oficina.Infrastructure.Queries.Vehicles;
using Oficina.Infrastructure.Repositories.Catalog;
using Oficina.Infrastructure.Repositories.Customers;
using Oficina.Infrastructure.Repositories.Identity;
using Oficina.Infrastructure.Repositories.Inventory;
using Oficina.Infrastructure.Repositories.Privacy;
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
        services.AddOptions<AddressLookupOptions>()
            .Bind(configuration.GetSection(AddressLookupOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient<IAddressLookup, ViaCepAddressLookup>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<AddressLookupOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
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
        services.AddScoped<Oficina.Domain.Quotes.Repositories.IEstimateRepository, Oficina.Infrastructure.Repositories.Quotes.EstimateRepository>();
        services.AddScoped<Oficina.Domain.Settings.Repositories.ISettingsRepository, Oficina.Infrastructure.Repositories.Quotes.SettingsRepository>();
        services.AddScoped<Oficina.Application.Quotes.Queries.IQuoteQueries, Oficina.Infrastructure.Queries.Quotes.QuoteQueries>();
        services.AddScoped<Oficina.Application.Abstractions.Links.IPublicLinks, Oficina.Infrastructure.Links.PublicLinks>();
        services.AddScoped<Oficina.Domain.Portal.Repositories.IPortalRepository, Oficina.Infrastructure.Repositories.Portal.PortalRepository>();
        services.AddScoped<Oficina.Domain.Documents.Repositories.IDocumentRepository, Oficina.Infrastructure.Repositories.Portal.DocumentRepository>();
        services.AddSingleton<Oficina.Application.Abstractions.Payments.IPixCodes, Oficina.Infrastructure.Payments.PixCodes>();
        services.AddScoped<Oficina.Domain.Audit.Repositories.IAuditRepository, Oficina.Infrastructure.Repositories.Audit.AuditRepository>();
        services.AddScoped<Oficina.Application.Settings.ISettingsQueries, Oficina.Infrastructure.Queries.Settings.SettingsQueries>();
        services.AddScoped<Oficina.Domain.Jobs.Repositories.IBackgroundJobRepository, Oficina.Infrastructure.Repositories.Jobs.BackgroundJobRepository>();
        services.AddScoped<Oficina.Application.Jobs.ICustomerInsightQueries, Oficina.Infrastructure.Queries.Jobs.CustomerInsightQueries>();
        services.AddScoped<Oficina.Infrastructure.Queries.Analytics.AnalyticsQueries>();
        services.AddScoped<Oficina.Application.Analytics.IAnalyticsQueries>(provider => provider.GetRequiredService<Oficina.Infrastructure.Queries.Analytics.AnalyticsQueries>());
        services.AddScoped<Oficina.Application.Dashboard.ISupplierQueries>(provider => provider.GetRequiredService<Oficina.Infrastructure.Queries.Analytics.AnalyticsQueries>());
        services.AddScoped<Oficina.Domain.Dashboard.IDashboardActionRepository, Oficina.Infrastructure.Repositories.Dashboard.DashboardActionRepository>();
        services.AddScoped<Oficina.Domain.Reports.ISavedReportRepository, Oficina.Infrastructure.Repositories.Reports.SavedReportRepository>();
        services.AddHostedService<Oficina.Infrastructure.Reports.ReportScheduleService>();
        services.AddScoped<Oficina.Infrastructure.Repositories.Operations.NotificationRepository>();
        services.AddScoped<Oficina.Domain.Notifications.INotificationRepository>(provider => provider.GetRequiredService<Oficina.Infrastructure.Repositories.Operations.NotificationRepository>());
        services.AddScoped<Oficina.Application.Notifications.IUserDirectory>(provider => provider.GetRequiredService<Oficina.Infrastructure.Repositories.Operations.NotificationRepository>());
        services.AddScoped<Oficina.Domain.Automations.IAutomationRepository, Oficina.Infrastructure.Repositories.Operations.AutomationRepository>();
        services.AddScoped<Oficina.Application.Automations.IAutomationQueries, Oficina.Infrastructure.Queries.Operations.AutomationQueries>();
        services.AddHostedService<Oficina.Infrastructure.Automations.AutomationSweepService>();
        services.AddScoped<Oficina.Application.Finance.IFinanceQueries, Oficina.Infrastructure.Queries.Finance.FinanceQueries>();
        services.AddOptions<Oficina.Infrastructure.Links.PublicLinksOptions>()
            .Bind(configuration.GetSection(Oficina.Infrastructure.Links.PublicLinksOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IStockCountRepository, StockCountRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<Oficina.Application.Inventory.Queries.IInventoryQueries, Oficina.Infrastructure.Queries.Inventory.InventoryQueries>();
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IPriceChangeRepository, PriceChangeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<ISensitiveDataAccessRepository, SensitiveDataAccessRepository>();
        services.AddScoped<IUserInvitationRepository, UserInvitationRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<ITeamProfileRepository, TeamProfileRepository>();
        services.AddScoped<ITeamQueries, TeamQueries>();
        services.AddScoped<ITwoFactorChallengeRepository, TwoFactorChallengeRepository>();
        services.AddScoped<IReauthenticationRepository, ReauthenticationRepository>();
        services.AddScoped<ICustomerQueries, CustomerQueries>();
        services.AddScoped<IServiceQueries, ServiceQueries>();
        services.AddScoped<IPartQueries, PartQueries>();
        services.AddScoped<IPackageQueries, PackageQueries>();
        services.AddScoped<IPriceHistoryQueries, PriceHistoryQueries>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IVehicleQueries, VehicleQueries>();
        services.AddScoped<ServiceOrderQueries>();
        services.AddScoped<IServiceOrderQueries>(provider => provider.GetRequiredService<ServiceOrderQueries>());
        services.AddScoped<IServiceOrderBoardQueries>(provider => provider.GetRequiredService<ServiceOrderQueries>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();

        services.AddOptions<DevelopmentAdminOptions>().Bind(configuration.GetSection(DevelopmentAdminOptions.SectionName));
        services.AddScoped<DevelopmentAdminSeeder>();

        services.AddScoped<INotifier, LogNotifier>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
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
