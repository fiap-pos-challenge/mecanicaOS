using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;

namespace Oficina.Infrastructure.Persistence.Seeding;

public sealed class TwoFactorSecretProtection(OficinaDbContext context, ISecretProtector protector, IUnitOfWork unitOfWork)
{
    public async Task<int> ProtectPlainSecretsAsync(CancellationToken cancellationToken)
    {
        var users = await context.Users.IgnoreQueryFilters()
            .Where(user => user.TwoFactorSecret != null && !user.TwoFactorSecret.StartsWith(DataProtectionPrefix))
            .ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            user.ProtectTwoFactorSecret(protector.Protect(user.TwoFactorSecret!));
        }

        if (users.Count > 0)
        {
            await unitOfWork.CommitOrThrowAsync(cancellationToken);
        }

        return users.Count;
    }

    private const string DataProtectionPrefix = Security.DataProtectionSecretProtector.Prefix;
}
