using FluentValidation;
using Oficina.Application;
using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Common.Behaviors;
using Oficina.Application.Identity.Invitations;
using Oficina.Application.Identity.Sessions;
using Oficina.Application.ServiceOrders.EventHandlers;
using Oficina.Domain.ServiceOrders.Events;

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
        services.AddScoped<IDomainEventHandler<ServiceOrderCompleted>, NotifyCustomerOnServiceOrderCompleted>();
        services.AddScoped<InvitationIssuer>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<Oficina.Application.Settings.SettingsStore>();
        services.AddScoped<Oficina.Application.Quotes.QuoteWorkflow>();
        services.AddScoped<Oficina.Application.Portal.PortalService>();
        services.AddScoped<Oficina.Application.Settings.SettingsService>();
        services.AddScoped<Oficina.Application.Jobs.JobService>();
        services.AddScoped<Oficina.Application.Dashboard.DashboardService>();
        services.AddScoped<Oficina.Application.Reports.ReportService>();
        services.AddScoped<Oficina.Application.Notifications.NotificationService>();
        services.AddScoped<Oficina.Application.Notifications.INotificationPublisher>(provider => provider.GetRequiredService<Oficina.Application.Notifications.NotificationService>());
        services.AddScoped<Oficina.Application.Inventory.PurchaseOrderDrafts>();
        services.AddScoped<Oficina.Application.Automations.AutomationService>();
        services.AddScoped<Oficina.Application.Finance.FinanceService>();
        services.AddScoped<IDomainEventHandler<Oficina.Domain.ServiceOrders.Events.ServiceOrderStatusChanged>, Oficina.Application.Automations.OnServiceOrderStatusChanged>();
        services.AddScoped<IDomainEventHandler<Oficina.Domain.ServiceOrders.Events.QuoteSent>, Oficina.Application.Automations.OnQuoteSent>();
        services.AddScoped<IDomainEventHandler<Oficina.Domain.ServiceOrders.Events.QuoteApproved>, Oficina.Application.Automations.OnQuoteApproved>();
        services.AddScoped<IDomainEventHandler<Oficina.Domain.ServiceOrders.Events.QuoteRejected>, Oficina.Application.Automations.OnQuoteRejected>();
        services.AddScoped<ReauthenticationGuard>();

        return services;
    }
}
