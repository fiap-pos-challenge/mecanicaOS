using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;

namespace Oficina.FunctionalTests.Api;

public sealed partial class QuotePortalApiTests
{
    private async Task<int> VersionAsync(HttpClient client, Guid orderId) =>
        (await ReadJsonAsync(await client.GetAsync($"/api/admin/service-orders/{orderId}", CancellationToken))).GetProperty("version").GetInt32();

    private async Task<JsonElement> DetailAsync(HttpClient client, Guid orderId) =>
        await ReadJsonAsync(await client.GetAsync($"/api/admin/service-orders/{orderId}", CancellationToken));

    private async Task<decimal> StockAsync(Guid partId) =>
        (decimal)(await Database.QueryAsync($"SELECT stockQuantity FROM parts WHERE id = '{partId}'"))[0]["stockQuantity"]!;

    [Fact]
    public async Task BoardRejection_RejectsTheQuote_SoALaterApprovalCannotOverrideIt()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (orderId, quoteId) = await QuoteAsync(admin, mechanic);
        await SendAsync(admin, quoteId);

        using var moved = await admin.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/transitions", new { to = "InDiagnosis", version = await VersionAsync(admin, orderId), reason = "Cliente pediu para rever" }, CancellationToken);
        Assert.True(moved.IsSuccessStatusCode, await moved.Content.ReadAsStringAsync(CancellationToken));

        var quote = await ReadJsonAsync(await admin.GetAsync($"/api/admin/quotes/{quoteId}", CancellationToken));
        Assert.Equal("Rejected", quote.GetProperty("status").GetString());
        Assert.Equal("Cliente pediu para rever", quote.GetProperty("rejectionReason").GetString());
        await AssertProblemAsync(await admin.PostAsync($"/api/admin/quotes/{quoteId}/approve", null, CancellationToken), HttpStatusCode.Conflict, "quotes.notPending");
        Assert.Equal("InDiagnosis", (await DetailAsync(admin, orderId)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task BoardApproval_ApprovesTheQuote_SoAPartialPortalAnswerIsRefused()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (orderId, partId) = await OrderInDiagnosisAsync(admin, mechanic);
        var quoteId = (await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/quotes", new { serviceOrderId = orderId }, CancellationToken))).GetProperty("id").GetGuid();
        var token = await SendAsync(admin, quoteId);

        using var moved = await admin.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/transitions", new { to = "InProgress", version = await VersionAsync(admin, orderId) }, CancellationToken);
        Assert.True(moved.IsSuccessStatusCode, await moved.Content.ReadAsStringAsync(CancellationToken));

        var quote = await ReadJsonAsync(await admin.GetAsync($"/api/admin/quotes/{quoteId}", CancellationToken));
        Assert.Equal("Approved", quote.GetProperty("status").GetString());
        Assert.Equal(9m, await StockAsync(partId));
        using var anonymous = Factory.CreateClient();
        var session = await ConfirmAsync(anonymous, token);
        var service = quote.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("kind").GetString() == "Service");
        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/quote/approve", session, new { itemIds = new[] { service.GetProperty("id").GetGuid() } }), CancellationToken),
            HttpStatusCode.Conflict, "portal.alreadyAnswered");
        var detail = await DetailAsync(admin, orderId);
        Assert.Single(detail.GetProperty("parts").EnumerateArray());
        Assert.Single(detail.GetProperty("services").EnumerateArray());
        Assert.Equal(9m, await StockAsync(partId));
    }

    [Fact]
    public async Task PartialPortalApproval_KeepsOnlyTheAcceptedItems_WithoutRecordingARejection()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (orderId, partId) = await OrderInDiagnosisAsync(admin, mechanic);
        var quote = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/quotes", new { serviceOrderId = orderId }, CancellationToken));
        var quoteId = quote.GetProperty("id").GetGuid();
        var token = await SendAsync(admin, quoteId);
        using var anonymous = Factory.CreateClient();
        var session = await ConfirmAsync(anonymous, token);
        var service = quote.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("kind").GetString() == "Service");

        using var approved = await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/quote/approve", session, new { itemIds = new[] { service.GetProperty("id").GetGuid() } }), CancellationToken);
        Assert.True(approved.IsSuccessStatusCode, await approved.Content.ReadAsStringAsync(CancellationToken));

        var detail = await DetailAsync(admin, orderId);
        Assert.Equal("InProgress", detail.GetProperty("status").GetString());
        Assert.Empty(detail.GetProperty("parts").EnumerateArray());
        Assert.Single(detail.GetProperty("services").EnumerateArray());
        Assert.Equal(120m, detail.GetProperty("totalAmount").GetDecimal());
        var statuses = detail.GetProperty("statusHistory").EnumerateArray().Select(entry => entry.GetProperty("newStatus").GetString()).ToArray();
        Assert.Equal(new[] { "Received", "InDiagnosis", "WaitingApproval", "InProgress" }, statuses);
        Assert.Equal(10m, await StockAsync(partId));
        Assert.Empty(await Database.QueryAsync("SELECT id FROM outboxMessages WHERE type = 'QuoteRejected'"));
    }

    [Fact]
    public async Task Approval_OutsideWaitingApproval_FailsInsteadOfReportingSuccess()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (orderId, _) = await OrderInDiagnosisAsync(admin, mechanic);
        Assert.True((await admin.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/transitions", new { to = "WaitingApproval", version = await VersionAsync(admin, orderId) }, CancellationToken)).IsSuccessStatusCode);
        Assert.True((await admin.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/transitions", new { to = "InProgress", version = await VersionAsync(admin, orderId) }, CancellationToken)).IsSuccessStatusCode);
        var quoteId = (await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/quotes", new { serviceOrderId = orderId }, CancellationToken))).GetProperty("id").GetGuid();

        await AssertProblemAsync(await admin.PostAsync($"/api/admin/quotes/{quoteId}/approve", null, CancellationToken), HttpStatusCode.Conflict, "quotes.notAwaitingApproval");
        Assert.Equal("Pending", (await ReadJsonAsync(await admin.GetAsync($"/api/admin/quotes/{quoteId}", CancellationToken))).GetProperty("status").GetString());
    }

    [Fact]
    public async Task LegacyApprovalRoute_GoesThroughTheQuote()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (orderId, quoteId) = await QuoteAsync(admin, mechanic);
        await SendAsync(admin, quoteId);

        using var approved = await admin.PostAsync($"/api/admin/service-orders/{orderId}/quote/approve", null, CancellationToken);
        Assert.True(approved.IsSuccessStatusCode, await approved.Content.ReadAsStringAsync(CancellationToken));

        Assert.Equal("Approved", (await ReadJsonAsync(await admin.GetAsync($"/api/admin/quotes/{quoteId}", CancellationToken))).GetProperty("status").GetString());
        Assert.Equal("InProgress", (await DetailAsync(admin, orderId)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task ParallelWrongDigits_NeverRegisterMoreThanThreeFailures()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (_, quoteId) = await QuoteAsync(admin, mechanic);
        var token = await SendAsync(admin, quoteId);
        using var anonymous = Factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(attempt => anonymous.PostAsJsonAsync("/api/public/portal/confirm", new { token, lastDigits = $"{attempt % 9}{attempt % 7}{attempt % 5}" }, CancellationToken)));

        var bodies = await Task.WhenAll(responses.Select(response => response.Content.ReadAsStringAsync(CancellationToken)));
        var failures = (int)(await Database.QueryAsync($"SELECT portalFailures FROM estimates WHERE id = '{quoteId}'"))[0]["portalFailures"]!;
        Assert.InRange(failures, 1, 3);
        Assert.DoesNotContain(bodies, body => body.Contains("\"Confirmed\"", StringComparison.Ordinal));
        Assert.True(responses.Count(response => response.StatusCode == HttpStatusCode.OK) <= 3);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task PublicRoutes_ShareARateLimitPerClient()
    {
        await using var limited = Factory.WithWebHostBuilder(builder => builder.UseSetting("Security:PublicRateLimit:PermitLimit", "3"));
        using var anonymous = limited.CreateClient();

        for (var request = 0; request < 3; request++)
        {
            Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/public/documents/NAO-EXISTE/verification", CancellationToken)).StatusCode);
        }

        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/public/portal/access", new { token = "x" }, CancellationToken), HttpStatusCode.TooManyRequests, "common.tooManyRequests");
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/public/payments/pix/webhook", new { txId = "x" }, CancellationToken), HttpStatusCode.TooManyRequests, "common.tooManyRequests");
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/public/service-orders/lookup", new { number = 1, document = "52998224725" }, CancellationToken), HttpStatusCode.TooManyRequests, "common.tooManyRequests");
    }

    [Fact]
    public async Task Photos_AreServedOnDemand_AndLimitedPerOrder()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (orderId, quoteId) = await QuoteAsync(admin, mechanic);
        const string Picture = "data:image/png;base64,iVBORw0KGgo=";
        for (var photo = 0; photo < 12; photo++)
        {
            using var added = await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/photos", new { clientId = $"foto-{photo}", fileName = $"foto-{photo}.png", contentType = "image/png", dataUrl = Picture }, CancellationToken);
            Assert.True(added.IsSuccessStatusCode, await added.Content.ReadAsStringAsync(CancellationToken));
        }

        await AssertProblemAsync(await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/photos", new { clientId = "foto-13", fileName = "foto.png", contentType = "image/png", dataUrl = Picture }, CancellationToken),
            HttpStatusCode.UnprocessableEntity, "serviceOrders.tooManyPhotos");
        Assert.True((await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/photos", new { clientId = "foto-0", fileName = "foto.png", contentType = "image/png", dataUrl = Picture }, CancellationToken)).IsSuccessStatusCode);

        var photos = (await DetailAsync(admin, orderId)).GetProperty("photos").EnumerateArray().ToList();
        Assert.Equal(12, photos.Count);
        var path = photos[0].GetProperty("url").GetString()!;
        Assert.StartsWith($"/api/admin/service-orders/{orderId}/photos/", path, StringComparison.Ordinal);
        Assert.DoesNotContain("data:", (await admin.GetStringAsync($"/api/admin/service-orders?page=1&pageSize=10", CancellationToken)), StringComparison.Ordinal);
        var content = await ReadJsonAsync(await mechanic.GetAsync(path, CancellationToken));
        Assert.Equal(Picture, content.GetProperty("dataUrl").GetString());
        await AssertProblemAsync(await admin.GetAsync($"/api/admin/service-orders/{orderId}/photos/{Guid.CreateVersion7()}", CancellationToken), HttpStatusCode.NotFound, "serviceOrders.photoNotFound");

        var token = await SendAsync(admin, quoteId);
        using var anonymous = Factory.CreateClient();
        var session = await ConfirmAsync(anonymous, token);
        var view = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, "/api/public/portal/order", session), CancellationToken));
        var portalPath = view.GetProperty("photos")[0].GetProperty("url").GetString()!;
        Assert.StartsWith("/api/public/portal/photos/", portalPath, StringComparison.Ordinal);
        await AssertProblemAsync(await anonymous.GetAsync(portalPath, CancellationToken), HttpStatusCode.Unauthorized, "portal.sessionExpired");
        var portalContent = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, portalPath, session), CancellationToken));
        Assert.Equal(Picture, portalContent.GetProperty("dataUrl").GetString());
    }
}
