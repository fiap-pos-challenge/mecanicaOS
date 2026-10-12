using System.Net;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class AuthorizationMatrixTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static readonly Role[] AdminOnly = { Role.Admin };
    private static readonly Role[] AdminOrAttendant = { Role.Admin, Role.Attendant };
    private static readonly Role[] Everyone = { Role.Admin, Role.Attendant, Role.Mechanic };
    private static readonly string AnyId = Guid.CreateVersion7().ToString();

    public static TheoryData<string, string, string?, string> Endpoints => new()
    {
        { "GET", "/api/auth/me", null, nameof(Everyone) },
        { "GET", "/api/admin/customers", null, nameof(AdminOrAttendant) },
        { "POST", "/api/admin/customers", """{"name":"Maria","document":"52998224725","phone":"11987654321"}""", nameof(AdminOrAttendant) },
        { "GET", $"/api/admin/customers/{AnyId}", null, nameof(AdminOrAttendant) },
        { "PUT", $"/api/admin/customers/{AnyId}", """{"name":"Maria","phone":"11987654321"}""", nameof(AdminOrAttendant) },
        { "POST", "/api/admin/customers/search", """{"document":"52998224725"}""", nameof(AdminOrAttendant) },
        { "DELETE", $"/api/admin/customers/{AnyId}", null, nameof(AdminOnly) },
        { "GET", "/api/admin/users", null, nameof(AdminOnly) },
        { "POST", "/api/admin/users", """{"name":"Novo","email":"novo@oficina.com","password":"Oficina@2026","role":"Mechanic"}""", nameof(AdminOnly) },
        { "GET", $"/api/admin/users/{AnyId}", null, nameof(AdminOnly) },
        { "PUT", $"/api/admin/users/{AnyId}", """{"name":"Novo","role":"Mechanic"}""", nameof(AdminOnly) },
        { "DELETE", $"/api/admin/users/{AnyId}", null, nameof(AdminOnly) },
        { "POST", $"/api/admin/users/{AnyId}/deactivate", null, nameof(AdminOnly) },
        { "POST", $"/api/admin/users/{AnyId}/reactivate", null, nameof(AdminOnly) },
        { "POST", "/api/admin/users/search", """{"email":"novo@oficina.com"}""", nameof(AdminOnly) },
        { "POST", "/api/admin/vehicles", """{"brand":"Fiat","model":"Uno","year": 2025,"customerId":"03df7661-6e42-4d05-bb81-e7aa3b1e0286"}""", nameof(AdminOrAttendant) },
        { "GET", $"/api/admin/vehicles/{AnyId}", null, nameof(Everyone) },
        { "GET", "/api/admin/vehicles", null, nameof(Everyone) },
        { "PUT", $"/api/admin/vehicles/{AnyId}", """{"brand":"Hyundai","model":"HB20","year": 2026}""", nameof(AdminOrAttendant) },
        { "DELETE", $"/api/admin/vehicles/{AnyId}", null, nameof(AdminOnly) },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Endpoint_Returns401WithoutToken403ForOtherRolesAndPassesForAllowedRoles(string method, string path, string? body, string allowed)
    {
        var allowedRoles = allowed switch
        {
            nameof(AdminOnly) => AdminOnly,
            nameof(AdminOrAttendant) => AdminOrAttendant,
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
