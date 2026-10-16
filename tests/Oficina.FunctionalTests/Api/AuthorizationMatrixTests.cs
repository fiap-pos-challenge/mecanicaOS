using System.Net;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class AuthorizationMatrixTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static readonly Role[] AdminOnly = { Role.Admin };
    private static readonly Role[] AdminOrManager = { Role.Admin, Role.Manager };
    private static readonly Role[] Staff = { Role.Admin, Role.Manager, Role.Attendant };
    private static readonly Role[] Everyone = { Role.Admin, Role.Manager, Role.Attendant, Role.Mechanic };
    private static readonly string AnyId = Guid.CreateVersion7().ToString();

    public static TheoryData<string, string, string?, string> Endpoints => new()
    {
        { "GET", "/api/auth/me", null, nameof(Everyone) },
        { "GET", "/api/admin/customers", null, nameof(Staff) },
        { "POST", "/api/admin/customers", """{"name":"Maria","document":"52998224725","phone":"11987654321"}""", nameof(Staff) },
        { "GET", $"/api/admin/customers/{AnyId}", null, nameof(Staff) },
        { "PUT", $"/api/admin/customers/{AnyId}", """{"name":"Maria","phone":"11987654321"}""", nameof(Staff) },
        { "POST", "/api/admin/customers/search", """{"document":"52998224725"}""", nameof(Staff) },
        { "DELETE", $"/api/admin/customers/{AnyId}", null, nameof(AdminOnly) },
        { "POST", "/api/admin/customers/query", """{"term":"Maria"}""", nameof(Staff) },
        { "POST", $"/api/admin/customers/{AnyId}/reveal", """{"field":"Phone","reason":"View"}""", nameof(Staff) },
        { "GET", "/api/admin/users", null, nameof(AdminOrManager) },
        { "GET", "/api/admin/users?role=Mechanic&status=Active", null, nameof(AdminOrManager) },
        { "POST", "/api/admin/users/invite", """{"name":"Novo","email":"convite@oficina.com","role":"Mechanic"}""", nameof(AdminOnly) },
        { "POST", $"/api/admin/users/{AnyId}/invite/resend", null, nameof(AdminOnly) },
        { "POST", "/api/admin/users", """{"name":"Novo","email":"novo@oficina.com","password":"Oficina@2026","role":"Mechanic"}""", nameof(AdminOnly) },
        { "GET", $"/api/admin/users/{AnyId}", null, nameof(AdminOrManager) },
        { "PUT", $"/api/admin/users/{AnyId}", """{"name":"Novo","role":"Mechanic"}""", nameof(AdminOnly) },
        { "DELETE", $"/api/admin/users/{AnyId}", null, nameof(AdminOnly) },
        { "POST", $"/api/admin/users/{AnyId}/deactivate", null, nameof(AdminOnly) },
        { "POST", $"/api/admin/users/{AnyId}/reactivate", null, nameof(AdminOnly) },
        { "POST", "/api/admin/users/search", """{"email":"novo@oficina.com"}""", nameof(AdminOnly) },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Endpoint_Returns401WithoutToken403ForOtherRolesAndPassesForAllowedRoles(string method, string path, string? body, string allowed)
    {
        var allowedRoles = allowed switch
        {
            nameof(AdminOnly) => AdminOnly,
            nameof(AdminOrManager) => AdminOrManager,
            nameof(Staff) => Staff,
            _ => Everyone,
        };

        using var anonymous = Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(anonymous, method, path, body)).StatusCode);

        foreach (var role in Everyone)
        {
            await Database.ResetAsync();
            using var client = await ClientAsync(role);
            var status = (await SendAsync(client, method, path, body)).StatusCode;
            if (allowedRoles.Contains(role))
            {
                Assert.True(status is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden, $"{role} {method} {path} = {status}");
                Assert.True((int)status < 500, $"{role} {method} {path} = {status}");
            }
            else
            {
                Assert.Equal(HttpStatusCode.Forbidden, status);
            }
        }
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string path, string? body)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body is not null)
        {
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        }

        return client.SendAsync(request, CancellationToken);
    }
}
