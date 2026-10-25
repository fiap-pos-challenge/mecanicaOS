using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;

namespace Oficina.FunctionalTests.Api;

public sealed partial class QuotePortalApiTests
{
    private static object[] DraftItems(JsonElement quote, decimal partQuantity) =>
        quote.GetProperty("items").EnumerateArray()
            .Select(item => (object)new
            {
                kind = item.GetProperty("kind").GetString(),
                itemId = item.GetProperty("itemId").GetGuid(),
                quantity = item.GetProperty("kind").GetString() == "Part" ? partQuantity : 1m,
            })
            .ToArray();

    private async Task<JsonElement> QuoteDetailAsync(HttpClient client, Guid quoteId) =>
        await ReadJsonAsync(await client.GetAsync($"/api/admin/quotes/{quoteId}", CancellationToken));

    [Fact]
    public async Task Draft_SavesItemsDiscountAndPaymentPlan_AndRejectsInvalidInput()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        using var attendant = await ClientAsync(Role.Attendant);
        var (_, quoteId) = await QuoteAsync(admin, mechanic);
        var quote = await QuoteDetailAsync(admin, quoteId);

        using var saved = await attendant.PutAsJsonAsync($"/api/admin/quotes/{quoteId}/draft", new
        {
            items = DraftItems(quote, 2m),
            discountMode = "Manual",
            manualDiscount = new { type = "Percent", value = 5m, reason = "Cliente antigo" },
            payment = new { methods = new[] { "Pix", "Cash", "Barter", "Pix" }, maxInstallments = 99 },
        }, CancellationToken);
        Assert.True(saved.IsSuccessStatusCode, await saved.Content.ReadAsStringAsync(CancellationToken));
        var draft = await ReadJsonAsync(saved);

        Assert.Equal(120m, draft.GetProperty("servicesTotal").GetDecimal());
        Assert.Equal(160m, draft.GetProperty("partsTotal").GetDecimal());
        Assert.Equal(14m, draft.GetProperty("discountTotal").GetDecimal());
        Assert.Equal(266m, draft.GetProperty("total").GetDecimal());
        Assert.Equal("Manual", draft.GetProperty("discountMode").GetString());
        Assert.Equal(["Pix", "Cash"], draft.GetProperty("payment").GetProperty("methods").EnumerateArray().Select(method => method.GetString()!).ToArray());
        Assert.Equal(6, draft.GetProperty("payment").GetProperty("maxInstallments").GetInt32());
        Assert.Equal(266m, (await QuoteDetailAsync(admin, quoteId)).GetProperty("total").GetDecimal());

        await AssertProblemAsync(await attendant.PutAsJsonAsync($"/api/admin/quotes/{quoteId}/draft", new
        {
            items = new[] { new { kind = "Gift", itemId = Guid.NewGuid(), quantity = 1m } },
            discountMode = "None",
        }, CancellationToken), HttpStatusCode.BadRequest, "quotes.invalidItem");
        await AssertProblemAsync(await attendant.PutAsJsonAsync($"/api/admin/quotes/{quoteId}/draft", new
        {
            items = DraftItems(quote, 0m),
            discountMode = "None",
        }, CancellationToken), HttpStatusCode.BadRequest, "quotes.invalidQuantity");
        await AssertProblemAsync(await attendant.PutAsJsonAsync($"/api/admin/quotes/{quoteId}/draft", new
        {
            items = DraftItems(quote, 1m),
            discountMode = "Manual",
            manualDiscount = new { type = "Amount", value = 0m, reason = "Cliente antigo" },
        }, CancellationToken), HttpStatusCode.BadRequest, "quotes.invalidDiscount");
        await AssertProblemAsync(await attendant.PutAsJsonAsync($"/api/admin/quotes/{Guid.NewGuid()}/draft", new
        {
            items = DraftItems(quote, 1m),
            discountMode = "None",
        }, CancellationToken), HttpStatusCode.NotFound, "quotes.notFound");
        await AssertProblemAsync(await attendant.PutAsJsonAsync($"/api/admin/quotes/{Guid.Empty}/draft", new
        {
            items = DraftItems(quote, 1m),
            discountMode = "None",
        }, CancellationToken), HttpStatusCode.BadRequest, "common.validationFailed");
    }

    [Fact]
    public async Task DiscountAboveTheLimit_NeedsAManagerDecision_ThatCanBeTakenOnlyOnce()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        using var attendant = await ClientAsync(Role.Attendant);
        using var manager = await ClientAsync(Role.Manager);
        var (_, quoteId) = await QuoteAsync(admin, mechanic);

        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/quotes/{quoteId}/discount-approvals", new { type = "Percent", value = 5m, reason = "Cliente antigo" }, CancellationToken),
            HttpStatusCode.UnprocessableEntity, "quotes.approvalNotNeeded");
        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/quotes/{quoteId}/discount-approvals", new { type = "Percent", value = 40m, reason = "abc" }, CancellationToken),
            HttpStatusCode.BadRequest, "quotes.discountReason");
        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/quotes/{quoteId}/discount-approvals", new { type = "Percent", value = 140m, reason = "Cliente de frota" }, CancellationToken),
            HttpStatusCode.BadRequest, "quotes.invalidDiscount");
        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/quotes/{Guid.NewGuid()}/discount-approvals", new { type = "Percent", value = 40m, reason = "Cliente de frota" }, CancellationToken),
            HttpStatusCode.NotFound, "quotes.notFound");

        using var requested = await attendant.PostAsJsonAsync($"/api/admin/quotes/{quoteId}/discount-approvals", new { type = "Percent", value = 40m, reason = "Cliente de frota" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, requested.StatusCode);
        var approval = await ReadJsonAsync(requested);
        Assert.Equal("Pending", approval.GetProperty("status").GetString());
        var approvalId = approval.GetProperty("id").GetGuid();

        using var forbidden = await attendant.PostAsync($"/api/admin/quotes/{quoteId}/discount-approvals/{approvalId}/approve", null, CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        await AssertProblemAsync(await manager.PostAsync($"/api/admin/quotes/{quoteId}/discount-approvals/{Guid.NewGuid()}/approve", null, CancellationToken), HttpStatusCode.NotFound, "quotes.notFound");

        var approved = await ReadJsonAsync(await manager.PostAsync($"/api/admin/quotes/{quoteId}/discount-approvals/{approvalId}/approve", null, CancellationToken));
        Assert.Equal("Approved", approved.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(approved.GetProperty("decidedBy").GetString()));
        await AssertProblemAsync(await manager.PostAsync($"/api/admin/quotes/{quoteId}/discount-approvals/{approvalId}/reject", null, CancellationToken), HttpStatusCode.Conflict, "quotes.approvalDecided");
        Assert.Equal("Approved", (await QuoteDetailAsync(admin, quoteId)).GetProperty("approval").GetProperty("status").GetString());

        Assert.Single(await Database.QueryAsync("SELECT id FROM auditEntries WHERE area = 'discounts'"));
    }

    [Fact]
    public async Task DeskRejection_LocksTheDraft_AndANewVersionCopiesTheItems()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        using var attendant = await ClientAsync(Role.Attendant);
        var (_, quoteId) = await QuoteAsync(admin, mechanic);
        var quote = await QuoteDetailAsync(admin, quoteId);

        await AssertProblemAsync(await attendant.PostAsync($"/api/admin/quotes/{quoteId}/versions", null, CancellationToken), HttpStatusCode.Conflict, "quotes.versionNotAllowed");
        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/quotes/{quoteId}/reject", new { reason = "x" }, CancellationToken), HttpStatusCode.BadRequest, "quotes.rejectReason");
        await AssertProblemAsync(await attendant.PostAsJsonAsync($"/api/admin/quotes/{Guid.NewGuid()}/reject", new { reason = "Cliente desistiu" }, CancellationToken), HttpStatusCode.NotFound, "quotes.notFound");

        var rejected = await ReadJsonAsync(await attendant.PostAsJsonAsync($"/api/admin/quotes/{quoteId}/reject", new { reason = "Cliente desistiu" }, CancellationToken));
        Assert.Equal("Rejected", rejected.GetProperty("status").GetString());
        Assert.Equal("Cliente desistiu", rejected.GetProperty("rejectionReason").GetString());
        await AssertProblemAsync(await attendant.PutAsJsonAsync($"/api/admin/quotes/{quoteId}/draft", new { items = DraftItems(quote, 1m), discountMode = "None" }, CancellationToken),
            HttpStatusCode.Conflict, "quotes.locked");

        using var created = await attendant.PostAsync($"/api/admin/quotes/{quoteId}/versions", null, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var next = await ReadJsonAsync(created);
        Assert.Equal(2, next.GetProperty("version").GetInt32());
        Assert.Equal("Pending", next.GetProperty("status").GetString());
        Assert.Equal(quote.GetProperty("number").GetInt64(), next.GetProperty("number").GetInt64());
        Assert.Equal(2, next.GetProperty("items").GetArrayLength());
        await AssertProblemAsync(await attendant.PostAsync($"/api/admin/quotes/{quoteId}/versions", null, CancellationToken), HttpStatusCode.Conflict, "quotes.versionNotAllowed");
        await AssertProblemAsync(await attendant.PostAsync($"/api/admin/quotes/{Guid.NewGuid()}/versions", null, CancellationToken), HttpStatusCode.NotFound, "quotes.notFound");

        var versions = await ReadJsonAsync(await attendant.GetAsync($"/api/admin/quotes/{next.GetProperty("id").GetGuid()}/versions", CancellationToken));
        Assert.Equal([2, 1], versions.GetProperty("items").EnumerateArray().Select(version => version.GetProperty("version").GetInt32()).ToArray());
        Assert.Equal("Rejected", versions.GetProperty("items")[1].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Retry_IsRefusedWhileTheLinkIsValid_AndSendsANewLinkAfterItExpires()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        using var attendant = await ClientAsync(Role.Attendant);
        var (_, quoteId) = await QuoteAsync(admin, mechanic);

        await AssertProblemAsync(await attendant.PostAsync($"/api/admin/quotes/{quoteId}/delivery/retry", null, CancellationToken), HttpStatusCode.NotFound, "quotes.notFound");
        var token = await SendAsync(admin, quoteId);
        await AssertProblemAsync(await attendant.PostAsync($"/api/admin/quotes/{quoteId}/delivery/retry", null, CancellationToken), HttpStatusCode.Conflict, "quotes.retryNotAllowed");

        await Database.QueryAsync($"UPDATE estimates SET delivery = JSON_MODIFY(delivery, '$.expiresAt', '2020-01-01T00:00:00+00:00') WHERE id = '{quoteId}'");
        using var retried = await attendant.PostAsync($"/api/admin/quotes/{quoteId}/delivery/retry", null, CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, retried.StatusCode);
        var delivery = await ReadJsonAsync(retried);
        var link = delivery.GetProperty("link").GetString()!;
        Assert.NotEqual(token, Uri.UnescapeDataString(link[(link.LastIndexOf('/') + 1)..]));
        Assert.True(delivery.GetProperty("expiresAt").GetDateTimeOffset() > TimeProvider.System.GetUtcNow());
        Assert.Equal(2, delivery.GetProperty("attempts").GetInt32());
    }
}
