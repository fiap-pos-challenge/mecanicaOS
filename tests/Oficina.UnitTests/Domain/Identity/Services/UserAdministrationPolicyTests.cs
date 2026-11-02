using NSubstitute;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Identity.Services;

namespace Oficina.UnitTests.Domain.Identity.Services;

public sealed class UserAdministrationPolicyTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly UserAdministrationPolicy _policy;
    private readonly Guid _actingAdminId = Guid.CreateVersion7();

    public UserAdministrationPolicyTests() => _policy = new UserAdministrationPolicy(_users);

    [Fact]
    public async Task EnsureCanDeactivate_OwnAccount_ReturnsCannotManageOwnAccount()
    {
        var self = CreateUser(Role.Admin);

        var result = await _policy.EnsureCanDeactivateAsync(self.Id, self, CancellationToken.None);

        Assert.Equal(IdentityErrors.CannotManageOwnAccount, result.Error);
    }

    [Fact]
    public async Task EnsureCanDeactivate_LastActiveAdmin_ReturnsLastActiveAdmin()
    {
        var target = CreateUser(Role.Admin);
        _users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _policy.EnsureCanDeactivateAsync(_actingAdminId, target, CancellationToken.None);

        Assert.Equal(IdentityErrors.LastActiveAdmin, result.Error);
    }

    [Fact]
    public async Task EnsureCanDeactivate_AdminWithAnotherActiveAdmin_Succeeds()
    {
        var target = CreateUser(Role.Admin);
        _users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(2);

        var result = await _policy.EnsureCanDeactivateAsync(_actingAdminId, target, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(Role.Attendant)]
    [InlineData(Role.Mechanic)]
    public async Task EnsureCanDeactivate_NonAdmin_SucceedsWithoutCountingAdmins(Role role)
    {
        var target = CreateUser(role);

        var result = await _policy.EnsureCanDeactivateAsync(_actingAdminId, target, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _users.DidNotReceive().CountActiveAdminsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureCanDeactivate_InactiveAdmin_DoesNotRequireAnotherAdmin()
    {
        var target = CreateUser(Role.Admin);
        target.Deactivate();

        var result = await _policy.EnsureCanDeactivateAsync(_actingAdminId, target, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _users.DidNotReceive().CountActiveAdminsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureCanDelete_OwnAccount_ReturnsCannotManageOwnAccount()
    {
        var self = CreateUser(Role.Admin);

        var result = await _policy.EnsureCanDeleteAsync(self.Id, self, CancellationToken.None);

        Assert.Equal(IdentityErrors.CannotManageOwnAccount, result.Error);
    }

    [Fact]
    public async Task EnsureCanDelete_LastActiveAdmin_ReturnsLastActiveAdmin()
    {
        var target = CreateUser(Role.Admin);
        _users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _policy.EnsureCanDeleteAsync(_actingAdminId, target, CancellationToken.None);

        Assert.Equal(IdentityErrors.LastActiveAdmin, result.Error);
    }

    [Fact]
    public async Task EnsureCanDelete_Mechanic_Succeeds()
    {
        var result = await _policy.EnsureCanDeleteAsync(_actingAdminId, CreateUser(Role.Mechanic), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureCanChangeRole_DemotingOwnAdminAccount_ReturnsCannotManageOwnAccount()
    {
        var self = CreateUser(Role.Admin);

        var result = await _policy.EnsureCanChangeRoleAsync(self.Id, self, Role.Attendant, CancellationToken.None);

        Assert.Equal(IdentityErrors.CannotManageOwnAccount, result.Error);
    }

    [Fact]
    public async Task EnsureCanChangeRole_KeepingOwnAdminRole_Succeeds()
    {
        var self = CreateUser(Role.Admin);

        var result = await _policy.EnsureCanChangeRoleAsync(self.Id, self, Role.Admin, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureCanChangeRole_DemotingLastActiveAdmin_ReturnsLastActiveAdmin()
    {
        var target = CreateUser(Role.Admin);
        _users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(1);

        var result = await _policy.EnsureCanChangeRoleAsync(_actingAdminId, target, Role.Mechanic, CancellationToken.None);

        Assert.Equal(IdentityErrors.LastActiveAdmin, result.Error);
    }

    [Fact]
    public async Task EnsureCanChangeRole_DemotingAdminWithAnotherActiveAdmin_Succeeds()
    {
        var target = CreateUser(Role.Admin);
        _users.CountActiveAdminsAsync(Arg.Any<CancellationToken>()).Returns(3);

        var result = await _policy.EnsureCanChangeRoleAsync(_actingAdminId, target, Role.Attendant, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureCanChangeRole_PromotingAttendantToAdmin_Succeeds()
    {
        var result = await _policy.EnsureCanChangeRoleAsync(_actingAdminId, CreateUser(Role.Attendant), Role.Admin, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _users.DidNotReceive().CountActiveAdminsAsync(Arg.Any<CancellationToken>());
    }

    private static User CreateUser(Role role) =>
        User.Create("Usuário de Teste", Email.Create($"{Guid.NewGuid():N}@oficina.com").Value, "hash", role).Value;
}
