using Oficina.Domain.Customers.Services;
using Oficina.Domain.Identity.Services;

namespace Oficina.Api.Extensions;

public static class DomainExtensions
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddScoped<RegistrationDeletionPolicy>();
        services.AddScoped<UserAdministrationPolicy>();

        return services;
    }
}
