using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Infrastructure.Persistence.Seeding;

public sealed class DevelopmentAdminSeeder(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    IOptions<DevelopmentAdminOptions> options)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Name) || string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            return;
        }

        var email = Email.Create(settings.Email).Value;
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return;
        }

        users.Add(User.Create(settings.Name, email, passwordHasher.Hash(settings.Password), Role.Admin).Value);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
