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
        { "GET", "/api/admin/services", null, nameof(Everyone) },
        { "POST", "/api/admin/services", """{"name":"Troca de óleo","basePrice":150,"estimatedMinutes":45}""", nameof(AdminOrManager) },
        { "GET", $"/api/admin/services/{AnyId}", null, nameof(Everyone) },
        { "PUT", $"/api/admin/services/{AnyId}", """{"name":"Troca de óleo","basePrice":150,"estimatedMinutes":45}""", nameof(AdminOrManager) },
        { "POST", $"/api/admin/services/{AnyId}/deactivate", null, nameof(AdminOrManager) },
        { "POST", $"/api/admin/services/{AnyId}/reactivate", null, nameof(AdminOrManager) },
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
        { "POST", "/api/admin/vehicles", """{"brand":"Fiat","model":"Uno","year": 2025,"customerId":"03df7661-6e42-4d05-bb81-e7aa3b1e0286"}""", nameof(Staff) },
        { "GET", $"/api/admin/vehicles/{AnyId}", null, nameof(Everyone) },
        { "GET", "/api/admin/vehicles", null, nameof(Everyone) },
        { "PUT", $"/api/admin/vehicles/{AnyId}", """{"brand":"Hyundai","model":"HB20","year": 2026}""", nameof(Staff) },
        { "DELETE", $"/api/admin/vehicles/{AnyId}", null, nameof(AdminOnly) },
        { "GET", $"/api/admin/customers/{AnyId}/vehicles", null, nameof(Everyone) },
        { "POST", "/api/admin/vehicles/search", """{"licensePlate":"BRA2E19"}""", nameof(Everyone) },
        { "POST", $"/api/admin/vehicles/{AnyId}/transfer", """{"newCustomerId":"03df7661-6e42-4d05-bb81-e7aa3b1e0286"}""", nameof(Staff) },
        { "GET", $"/api/admin/services/{AnyId}/price-history", null, nameof(Everyone) },
        { "GET", "/api/admin/quotes", null, nameof(Staff) },
        { "GET", "/api/admin/quotes/service-orders", null, nameof(Staff) },
        { "POST", "/api/admin/quotes", $$"""{"serviceOrderId":"{{AnyId}}"}""", nameof(Staff) },
        { "GET", $"/api/admin/quotes/{AnyId}", null, nameof(Staff) },
        { "GET", $"/api/admin/quotes/{AnyId}/options", null, nameof(Staff) },
        { "POST", $"/api/admin/quotes/{AnyId}/send", """{"channel":"Link","expiresInDays":7,"message":"Oi"}""", nameof(Staff) },
        { "GET", $"/api/admin/quotes/{AnyId}/versions", null, nameof(Staff) },
        { "GET", "/api/admin/documents/OS-1-ABCDEF", null, nameof(Staff) },
        { "GET", "/api/admin/documents/OS-1-ABCDEF/pdf", null, nameof(Staff) },
        { "POST", "/api/admin/documents/OS-1-ABCDEF/send", """{"channel":"Email"}""", nameof(Staff) },
        { "GET", "/api/admin/settings/workshop", null, nameof(AdminOrManager) },
        { "PUT", "/api/admin/settings/workshop", """{"name":"Oficina"}""", nameof(AdminOnly) },
        { "GET", "/api/admin/settings/discounts", null, nameof(AdminOrManager) },
        { "GET", "/api/admin/settings/payments", null, nameof(AdminOrManager) },
        { "GET", "/api/admin/settings/messages", null, nameof(AdminOrManager) },
        { "GET", "/api/admin/settings/security", null, nameof(AdminOnly) },
        { "GET", "/api/admin/settings/sessions", null, nameof(AdminOnly) },
        { "POST", "/api/admin/settings/audit/search", """{"area":"all","page":1,"pageSize":20}""", nameof(AdminOnly) },
        { "GET", "/api/admin/settings/fiscal", null, nameof(AdminOrManager) },
        { "POST", "/api/admin/customers/summaries", """{"customerIds":[]}""", nameof(Staff) },
        { "GET", $"/api/admin/customers/{AnyId}/service-history", null, nameof(Staff) },
        { "POST", $"/api/admin/customers/{AnyId}/data-export", null, nameof(AdminOrManager) },
        { "POST", "/api/admin/imports/customers", """{"fileName":"a.csv","csv":"nome"}""", nameof(Staff) },
        { "GET", $"/api/admin/jobs/{AnyId}", null, nameof(Staff) },
        { "GET", "/api/admin/dashboard", null, nameof(Staff) },
        { "GET", "/api/admin/dashboard/layout", null, nameof(Staff) },
        { "POST", "/api/admin/dashboard/attention/maintenance/actions", """{"action":"SendReminder"}""", nameof(Staff) },
        { "POST", "/api/admin/reports/run", """{"definition":{"measure":"revenue","groupBy":"month","period":{"preset":"thisMonth","from":null,"to":null},"chart":"column"}}""", nameof(AdminOrManager) },
        { "GET", "/api/admin/reports/saved", null, nameof(AdminOrManager) },
        { "POST", "/api/admin/reports/exports", """{"definition":{"measure":"revenue","groupBy":"month","period":{"preset":"thisMonth","from":null,"to":null},"chart":"column"},"format":"pdf"}""", nameof(AdminOrManager) },
        { "POST", "/api/admin/assistant/report-definitions", """{"question":"faturamento por mês"}""", nameof(AdminOrManager) },
        { "GET", "/api/admin/automations/rules", null, nameof(AdminOrManager) },
        { "GET", "/api/admin/automations/queues", null, nameof(AdminOrManager) },
        { "POST", $"/api/admin/automations/queues/failures/{AnyId}/retry", null, nameof(AdminOnly) },
        { "GET", "/api/admin/notifications", null, nameof(Everyone) },
        { "GET", "/api/admin/notifications/preferences", null, nameof(Everyone) },
        { "GET", "/api/admin/finance", null, nameof(AdminOrManager) },
        { "GET", "/api/admin/documents", null, nameof(Staff) },
        { "GET", "/api/admin/platform", null, nameof(AdminOnly) },
        { "GET", "/api/admin/parts", null, nameof(Everyone) },
        { "POST", "/api/addresses/lookup", """{"zipCode":"01310100"}""", nameof(Staff) },
        { "POST", "/api/admin/parts", """{"code":"PEC-1","name":"Filtro","type":"Part","unitOfMeasure":"un","unitPrice":10,"cost":5,"minimumStock":0}""", nameof(AdminOrManager) },
        { "GET", $"/api/admin/parts/{AnyId}", null, nameof(Everyone) },
        { "GET", $"/api/admin/parts/{AnyId}/price-history", null, nameof(Everyone) },
        { "PUT", $"/api/admin/parts/{AnyId}", """{"code":"PEC-1","name":"Filtro","type":"Part","unitOfMeasure":"un","unitPrice":10,"cost":5,"minimumStock":0}""", nameof(AdminOrManager) },
        { "POST", $"/api/admin/parts/{AnyId}/deactivate", null, nameof(AdminOrManager) },
        { "POST", $"/api/admin/parts/{AnyId}/reactivate", null, nameof(AdminOrManager) },
        { "GET", "/api/admin/packages", null, nameof(Everyone) },
        { "POST", "/api/admin/packages", """{"name":"Pacote","items":[],"price":10}""", nameof(AdminOrManager) },
        { "GET", $"/api/admin/packages/{AnyId}", null, nameof(Everyone) },
        { "GET", $"/api/admin/packages/{AnyId}/price-history", null, nameof(Everyone) },
        { "PUT", $"/api/admin/packages/{AnyId}", """{"name":"Pacote","items":[],"price":10}""", nameof(AdminOrManager) },
        { "POST", $"/api/admin/packages/{AnyId}/deactivate", null, nameof(AdminOrManager) },
        { "POST", $"/api/admin/packages/{AnyId}/reactivate", null, nameof(AdminOrManager) },
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
