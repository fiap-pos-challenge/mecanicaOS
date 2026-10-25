using FluentValidation;
using Oficina.Application;
using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Automations;
using Oficina.Application.Common.Behaviors;
using Oficina.Application.Dashboard;
using Oficina.Application.Documents;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.Inventory;
using Oficina.Application.Jobs;
using Oficina.Application.Notifications;
using Oficina.Application.Portal;
using Oficina.Application.Quotes.Decisions;
using Oficina.Application.Quotes.Delivery;
using Oficina.Application.Quotes.Presenters;
using Oficina.Application.Quotes.Pricing;
using Oficina.Application.Reports;
using Oficina.Application.Settings;

namespace Oficina.Api.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(AssemblyReference.Assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(AssemblyReference.Assembly, includeInternalTypes: true);
        services.AddDomainEventHandlers();
        services.AddScoped<InvitationIssuer>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<ReauthenticationGuard>();
        services.AddScoped<SettingsStore>();
        services.AddScoped<SettingsAudit>();
        services.AddScoped<QuotePricing>();
        services.AddScoped<QuotePresenter>();
        services.AddScoped<EstimateScopeResolver>();
        services.AddScoped<QuoteDecider>();
        services.AddScoped<QuoteDispatcher>();
        services.AddScoped<DocumentIssuer>();
        services.AddScoped<PortalAccess>();
        services.AddScoped<PortalOrderView>();
        services.AddScoped<ITeamNotificationPublisher, NotificationPublisher>();
        services.AddScoped<NotificationPreferences>();
        services.AddScoped<PurchaseOrderDrafts>();
        services.AddScoped<AutomationScheduler>();
        services.AddScoped<AutomationRunner>();
        services.AddScoped<DashboardLayouts>();
        services.AddScoped<JobPresenter>();
        services.AddScoped<CustomerCsvImporter>();
        services.AddScoped<ReportCalculator>();
        services.AddScoped<ScheduledReportSender>();

        return services;
    }

    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        var handlers = AssemblyReference.Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                .Select(contract => (Contract: contract, Implementation: type)));
        foreach (var (contract, implementation) in handlers)
        {
            services.AddScoped(contract, implementation);
        }
    }
}
