using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class InvitationsApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private const string Password = "Oficina@2026";

    [Fact]
    public async Task Invite_ThenAccept_LetsThePersonSignInWithTheirOwnPassword()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var anonymous = Factory.CreateClient();

        using var invited = await admin.PostAsJsonAsync("/api/admin/users/invite", new { name = "Bruna Oliveira", email = "bruna@oficina.com", role = "Manager" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, invited.StatusCode);
        var user = await ReadJsonAsync(invited);
        Assert.Equal("InvitationPending", user.GetProperty("status").GetString());
        Assert.Equal("Manager", user.GetProperty("role").GetString());
        Assert.True(user.GetProperty("invitationExpiresAt").GetDateTimeOffset() > TimeProvider.System.GetUtcNow().AddHours(71));

        var token = Assert.Single(Factory.Emails.Invitations).Token;
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "bruna@oficina.com", password = Password }, CancellationToken), HttpStatusCode.Unauthorized, IdentityErrors.InvalidCredentials.Code);

        using var inspected = await anonymous.PostAsJsonAsync("/api/auth/invitations/inspect", new { token }, CancellationToken);
        var invitation = await ReadJsonAsync(inspected);
        Assert.Equal("Bruna Oliveira", invitation.GetProperty("name").GetString());
        Assert.Equal("b•••@oficina.com", invitation.GetProperty("maskedEmail").GetString());

        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/auth/invitations/accept", new { token, password = "fraca" }, CancellationToken), HttpStatusCode.BadRequest, CommonErrors.ValidationFailed.Code);
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.PostAsJsonAsync("/api/auth/invitations/accept", new { token, password = Password }, CancellationToken)).StatusCode);
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/auth/invitations/accept", new { token, password = Password }, CancellationToken), HttpStatusCode.Conflict, IdentityErrors.InvitationAlreadyUsed.Code);

        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = "bruna@oficina.com", password = Password }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        using var listed = await admin.GetAsync("/api/admin/users?status=Active&role=Manager", CancellationToken);
        var item = Assert.Single((await ReadJsonAsync(listed)).GetProperty("items").EnumerateArray());
        Assert.Equal("Active", item.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("invitationExpiresAt").ValueKind);
    }

    [Fact]
    public async Task Resend_ReplacesTheOldLinkAndDuplicateEmailIsRejected()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var anonymous = Factory.CreateClient();
        var body = new { name = "Rafael Costa", email = "rafael@oficina.com", role = "Mechanic" };
        var id = (await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/users/invite", body, CancellationToken))).GetProperty("id").GetGuid();

        await AssertProblemAsync(await admin.PostAsJsonAsync("/api/admin/users/invite", body, CancellationToken), HttpStatusCode.Conflict, IdentityErrors.DuplicateEmail.Code);

        using var pending = await admin.GetAsync("/api/admin/users?status=InvitationPending", CancellationToken);
        var pendingUser = Assert.Single((await ReadJsonAsync(pending)).GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.String, pendingUser.GetProperty("invitationExpiresAt").ValueKind);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/users/{id}/invite/resend", null, CancellationToken)).StatusCode);
        var tokens = Factory.Emails.Invitations.Select(message => message.Token).ToArray();
        Assert.Equal(2, tokens.Length);

        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/auth/invitations/inspect", new { token = tokens[0] }, CancellationToken), HttpStatusCode.Conflict, IdentityErrors.InvitationAlreadyUsed.Code);
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/auth/invitations/inspect", new { token = "desconhecido" }, CancellationToken), HttpStatusCode.NotFound, IdentityErrors.InvitationNotFound.Code);
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.PostAsJsonAsync("/api/auth/invitations/accept", new { token = tokens[1], password = Password }, CancellationToken)).StatusCode);
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/users/{id}/invite/resend", null, CancellationToken), HttpStatusCode.Conflict, IdentityErrors.InvitationNotPending.Code);
    }

    [Fact]
    public async Task Manager_ReadsTheTeamButCannotInvite()
    {
        using var manager = await ClientAsync(Role.Manager);

        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/admin/users", CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PostAsJsonAsync("/api/admin/users/invite", new { name = "Xavier", email = "x@oficina.com", role = "Mechanic" }, CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/admin/customers", CancellationToken)).StatusCode);
    }
}
