using System.Net;
using System.Net.Http.Json;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class UsersApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    [Fact]
    public async Task Crud_CreatesAnyRoleUpdatesDeactivatesReactivatesAndSoftDeletes()
    {
        using var admin = await ClientAsync(Role.Admin);

        using var created = await admin.PostAsJsonAsync("/api/admin/users", new { name = "Carlos Mecânico", email = "carlos@oficina.com", password = "Oficina@2026", role = "Mechanic" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await ReadJsonAsync(created);
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal("Mechanic", body.GetProperty("role").GetString());
        Assert.False(body.TryGetProperty("passwordHash", out _));

        using var updated = await admin.PutAsJsonAsync($"/api/admin/users/{id}", new { name = "Carlos Souza", role = "Admin" }, CancellationToken);
        Assert.Equal("Admin", (await ReadJsonAsync(updated)).GetProperty("role").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/users/{id}/deactivate", null, CancellationToken)).StatusCode);
        Assert.False((await ReadJsonAsync(await admin.GetAsync($"/api/admin/users/{id}", CancellationToken))).GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/users/{id}/reactivate", null, CancellationToken)).StatusCode);

        using var found = await admin.PostAsJsonAsync("/api/admin/users/search", new { email = "CARLOS@oficina.com" }, CancellationToken);
        Assert.Equal(id, (await ReadJsonAsync(found)).GetProperty("id").GetGuid());
        Assert.Equal(2, (await ReadJsonAsync(await admin.GetAsync("/api/admin/users", CancellationToken))).GetProperty("totalItems").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/users/{id}", CancellationToken)).StatusCode);
        var row = Assert.Single(await Database.QueryAsync($"SELECT deletedAt, isActive FROM users WHERE id = '{id}'"));
        Assert.NotNull(row["deletedAt"]);
        Assert.False((bool)row["isActive"]!);
    }

    [Fact]
    public async Task Admin_CannotDeactivateThemselves()
    {
        var self = await SaveUserAsync(Role.Admin);
        await SaveUserAsync(Role.Admin);
        using var client = Factory.CreateClient(self);

        await AssertProblemAsync(await client.PostAsync($"/api/admin/users/{self.Id}/deactivate", null, CancellationToken), HttpStatusCode.UnprocessableEntity, IdentityErrors.CannotManageOwnAccount.Code);
    }

    [Fact]
    public async Task LastActiveAdmin_CannotBeDemoted()
    {
        var lastAdmin = await SaveUserAsync(Role.Admin);
        using var client = Factory.CreateClient(await SaveUserAsync(Role.Admin));
        await Database.QueryAsync($"UPDATE users SET isActive = 0 WHERE id <> '{lastAdmin.Id}'; SELECT 1 AS done");

        using var response = await client.PutAsJsonAsync($"/api/admin/users/{lastAdmin.Id}", new { name = "Ana", role = "Attendant" }, CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, IdentityErrors.LastActiveAdmin.Code);
    }

    [Fact]
    public async Task Create_DuplicateEmailAndWeakPassword_AreRejected()
    {
        using var admin = await ClientAsync(Role.Admin);
        var user = new { name = "Beatriz", email = "bia@oficina.com", password = "Oficina@2026", role = "Attendant" };
        await admin.PostAsJsonAsync("/api/admin/users", user, CancellationToken);

        await AssertProblemAsync(await admin.PostAsJsonAsync("/api/admin/users", user, CancellationToken), HttpStatusCode.Conflict, IdentityErrors.DuplicateEmail.Code);
        await AssertProblemAsync(await admin.PostAsJsonAsync("/api/admin/users", user with { email = "outra@oficina.com", password = "fraca" }, CancellationToken), HttpStatusCode.BadRequest, CommonErrors.ValidationFailed.Code);
    }
}
