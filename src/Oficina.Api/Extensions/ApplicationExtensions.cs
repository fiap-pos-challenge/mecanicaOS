using FluentValidation;
using Oficina.Application;
using Oficina.Application.Abstractions.Messaging;
using Oficina.Application.Common.Behaviors;
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

        return services;
    }
}
