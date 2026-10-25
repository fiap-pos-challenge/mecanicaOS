using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Domain.Identity.Services;

public sealed class UserAdministrationPolicy(IUserRepository users)
{
    private const int MinimumActiveAdmins = 1;

    public Task<Result> EnsureCanDeactivateAsync(Guid actingUserId, User target, CancellationToken cancellationToken) =>
        EnsureCanRemoveAccessAsync(actingUserId, target, cancellationToken);

    public Task<Result> EnsureCanDeleteAsync(Guid actingUserId, User target, CancellationToken cancellationToken) =>
        EnsureCanRemoveAccessAsync(actingUserId, target, cancellationToken);

    public async Task<Result> EnsureCanChangeRoleAsync(Guid actingUserId, User target, Role newRole, CancellationToken cancellationToken)
    {
        var isDemotingAdmin = target.Role == Role.Admin && newRole != Role.Admin;
        if (!isDemotingAdmin)
        {
            return Result.Success();
        }

        if (target.Id == actingUserId)
        {
            return IdentityErrors.CannotManageOwnAccount;
        }

        return await EnsureAnotherActiveAdminRemainsAsync(target, cancellationToken);
    }

    private async Task<Result> EnsureCanRemoveAccessAsync(Guid actingUserId, User target, CancellationToken cancellationToken)
    {
        if (target.Id == actingUserId)
        {
            return IdentityErrors.CannotManageOwnAccount;
        }

        return await EnsureAnotherActiveAdminRemainsAsync(target, cancellationToken);
    }

    private async Task<Result> EnsureAnotherActiveAdminRemainsAsync(User target, CancellationToken cancellationToken)
    {
        if (!target.IsActiveAdmin)
        {
            return Result.Success();
        }

        var activeAdmins = await users.CountActiveAdminsAsync(cancellationToken);
        return activeAdmins > MinimumActiveAdmins ? Result.Success() : IdentityErrors.LastActiveAdmin;
    }
}
