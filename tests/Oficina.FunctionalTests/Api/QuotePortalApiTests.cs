using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed partial class QuotePortalApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private const string Session = "X-Portal-Session";

    private async Task<(Guid OrderId, Guid PartId)> OrderInDiagnosisAsync(HttpClient admin, HttpClient mechanic)
    {
        var customer = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/customers", new
        {
            name = "Maria Souza",
            document = "529.982.247-25",
            phone = "(11) 98765-4321",
            email = "maria@exemplo.com",
            messagingConsent = new { whatsApp = true, email = true },
        }, CancellationToken));
        var customerId = customer.GetProperty("id").GetGuid();
        var vehicle = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/vehicles", new { customerId, licensePlate = "BRA2E19", brand = "Fiat", model = "Uno", year = 2020, mileage = 50_000 }, CancellationToken));
        var service = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/services", new { name = "Alinhamento 3D", basePrice = 120m, estimatedMinutes = 40, commissionPercent = 10m, warrantyDays = 90 }, CancellationToken));
        var part = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/parts", new { code = "PST-001", name = "Pastilha de freio dianteira", type = "Part", unitOfMeasure = "un", unitPrice = 80m, cost = 40m, minimumStock = 0m }, CancellationToken));
        var partId = part.GetProperty("id").GetGuid();
        await Database.QueryAsync($"UPDATE parts SET stockQuantity = 10 WHERE id = '{partId}'");
        var order = await ReadJsonAsync(await admin.PostAsJsonAsync("/api/admin/service-orders", new { customerId, vehicleId = vehicle.GetProperty("id").GetGuid(), problemDescription = "Freio chiando" }, CancellationToken));
        var orderId = order.GetProperty("id").GetGuid();
        await mechanic.PostAsync($"/api/admin/service-orders/{orderId}/start-service", null, CancellationToken);
        using var sent = await mechanic.PostAsJsonAsync($"/api/admin/service-orders/{orderId}/send-to-quote", new
        {
            description = "Trocar pastilha e alinhar",
            items = new object[] { new { kind = "Service", itemId = service.GetProperty("id").GetGuid() }, new { kind = "Part", itemId = partId } },
        }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
        return (orderId, partId);
    }

    private async Task<(Guid OrderId, Guid QuoteId)> QuoteAsync(HttpClient admin, HttpClient mechanic)
    {
        var (orderId, _) = await OrderInDiagnosisAsync(admin, mechanic);
        var open = await ReadJsonAsync(await admin.GetAsync("/api/admin/quotes/service-orders", CancellationToken));
        Assert.Contains(open.GetProperty("items").EnumerateArray(), item => item.GetProperty("id").GetGuid() == orderId);
        using var created = await admin.PostAsJsonAsync("/api/admin/quotes", new { serviceOrderId = orderId }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var quote = await ReadJsonAsync(created);
        await AssertProblemAsync(await admin.PostAsJsonAsync("/api/admin/quotes", new { serviceOrderId = orderId }, CancellationToken), HttpStatusCode.Conflict, "quotes.alreadyExists");
        return (orderId, quote.GetProperty("id").GetGuid());
    }

    private async Task<string> SendAsync(HttpClient admin, Guid quoteId)
    {
        var draft = await ReadJsonAsync(await admin.PostAsJsonAsync($"/api/admin/quotes/{quoteId}/message-draft", new { channel = "Link" }, CancellationToken));
        using var sent = await admin.PostAsJsonAsync($"/api/admin/quotes/{quoteId}/send", new { channel = "Link", expiresInDays = 7, message = draft.GetProperty("message").GetString() }, CancellationToken);
        Assert.True(sent.IsSuccessStatusCode, await sent.Content.ReadAsStringAsync(CancellationToken));
        var link = (await ReadJsonAsync(sent)).GetProperty("delivery").GetProperty("link").GetString()!;
        return Uri.UnescapeDataString(link[(link.LastIndexOf('/') + 1)..]);
    }

    private static async Task<string> ConfirmAsync(HttpClient client, string token)
    {
        var confirmed = await ReadJsonAsync(await client.PostAsJsonAsync("/api/public/portal/confirm", new { token, lastDigits = "321" }, CancellationToken));
        Assert.Equal("Confirmed", confirmed.GetProperty("status").GetString());
        return confirmed.GetProperty("session").GetString()!;
    }

    private static HttpRequestMessage Portal(HttpMethod method, string url, string session, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(Session, session);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private async Task SavePixKeyAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var settings = scope.ServiceProvider.GetRequiredService<SettingsStore>();
        await settings.SaveAsync(SettingsKeys.Payments, SettingsDefaults.Payments with { PixKey = "pix@oficina.com" }, CancellationToken);
        Assert.True((await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(CancellationToken)).IsSuccess);
    }

    [Fact]
    public async Task Quote_FromDraftToPortalApprovalPixPaymentAndDocuments()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (orderId, quoteId) = await QuoteAsync(admin, mechanic);

        var quote = await ReadJsonAsync(await admin.GetAsync($"/api/admin/quotes/{quoteId}", CancellationToken));
        Assert.Equal("Pending", quote.GetProperty("status").GetString());
        Assert.Equal(2, quote.GetProperty("items").GetArrayLength());
        Assert.Equal(200m, quote.GetProperty("total").GetDecimal());
        var options = await ReadJsonAsync(await admin.GetAsync($"/api/admin/quotes/{quoteId}/options", CancellationToken));
        Assert.Contains(options.GetProperty("expirationDays").EnumerateArray(), day => day.GetInt32() == 7);
        Assert.Single(options.GetProperty("services").EnumerateArray());
        var list = await ReadJsonAsync(await admin.GetAsync("/api/admin/quotes?page=1&pageSize=10", CancellationToken));
        Assert.Equal(1, list.GetProperty("totalItems").GetInt32());

        var token = await SendAsync(admin, quoteId);
        var delivery = await ReadJsonAsync(await admin.GetAsync($"/api/admin/quotes/{quoteId}/delivery", CancellationToken));
        Assert.Equal("Link", delivery.GetProperty("channel").GetString());

        using var anonymous = Factory.CreateClient();
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/public/portal/access", new { token = "desconhecido" }, CancellationToken), HttpStatusCode.NotFound, "portal.linkUnavailable");
        var access = await ReadJsonAsync(await anonymous.PostAsJsonAsync("/api/public/portal/access", new { token }, CancellationToken));
        Assert.Equal(3, access.GetProperty("attemptsLeft").GetInt32());
        var wrong = await ReadJsonAsync(await anonymous.PostAsJsonAsync("/api/public/portal/confirm", new { token, lastDigits = "000" }, CancellationToken));
        Assert.Equal("WrongDigits", wrong.GetProperty("status").GetString());
        Assert.Equal(2, wrong.GetProperty("attemptsLeft").GetInt32());
        var session = await ConfirmAsync(anonymous, token);

        await AssertProblemAsync(await anonymous.GetAsync("/api/public/portal/order", CancellationToken), HttpStatusCode.Unauthorized, "portal.sessionExpired");
        var view = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, "/api/public/portal/order", session), CancellationToken));
        Assert.Equal("Maria", view.GetProperty("customerFirstName").GetString());
        Assert.Equal("Pending", view.GetProperty("quote").GetProperty("status").GetString());
        var items = view.GetProperty("quote").GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(view.GetProperty("documents").EnumerateArray(), document => document.GetProperty("kind").GetString() == "Quote");

        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/quote/approve", session, new { itemIds = Array.Empty<Guid>() }), CancellationToken), HttpStatusCode.BadRequest, "portal.noItems");
        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/payments/pix", session), CancellationToken), HttpStatusCode.UnprocessableEntity, "portal.nothingToPay");
        var service = items.Single(item => item.GetProperty("kind").GetString() == "Service");
        using var approvedResponse = await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/quote/approve", session, new { itemIds = new[] { service.GetProperty("id").GetGuid() } }), CancellationToken);
        Assert.True(approvedResponse.IsSuccessStatusCode, await approvedResponse.Content.ReadAsStringAsync(CancellationToken));
        var approved = await ReadJsonAsync(approvedResponse);
        Assert.Equal("Approved", approved.GetProperty("quote").GetProperty("status").GetString());
        Assert.Equal(120m, approved.GetProperty("payment").GetProperty("amountDue").GetDecimal());
        Assert.Equal("InProgress", approved.GetProperty("status").GetString());
        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/quote/reject", session, new { reason = "Muito caro" }), CancellationToken), HttpStatusCode.Conflict, "portal.alreadyAnswered");

        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/payments/pix", session), CancellationToken), HttpStatusCode.UnprocessableEntity, "portal.pixUnavailable");
        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/payments/card", session), CancellationToken), HttpStatusCode.UnprocessableEntity, "portal.cardUnavailable");
        await SavePixKeyAsync();
        var pix = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/payments/pix", session), CancellationToken));
        Assert.StartsWith("000201", pix.GetProperty("copyPaste").GetString(), StringComparison.Ordinal);
        Assert.StartsWith("data:image/png;base64,", pix.GetProperty("qrCodeDataUrl").GetString(), StringComparison.Ordinal);
        var paymentId = pix.GetProperty("paymentId").GetGuid();
        var pending = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, $"/api/public/portal/payments/{paymentId}", session), CancellationToken));
        Assert.Equal("Pending", pending.GetProperty("status").GetString());
        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, $"/api/public/portal/payments/{Guid.CreateVersion7()}", session), CancellationToken), HttpStatusCode.NotFound, "portal.paymentNotFound");

        var txId = (string)(await Database.QueryAsync($"SELECT txId FROM portalCharges WHERE id = '{paymentId}'"))[0]["txId"]!;
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(FunctionalApiFactory.PixWebhookSecret), Encoding.UTF8.GetBytes(txId)));
        using var badWebhook = new HttpRequestMessage(HttpMethod.Post, "/api/public/payments/pix/webhook") { Content = JsonContent.Create(new { txId }) };
        badWebhook.Headers.Add("X-Pix-Signature", "00");
        await AssertProblemAsync(await anonymous.SendAsync(badWebhook, CancellationToken), HttpStatusCode.Unauthorized, "portal.invalidWebhook");
        using var webhook = new HttpRequestMessage(HttpMethod.Post, "/api/public/payments/pix/webhook") { Content = JsonContent.Create(new { txId }) };
        webhook.Headers.Add("X-Pix-Signature", signature);
        Assert.Equal(HttpStatusCode.NoContent, (await anonymous.SendAsync(webhook, CancellationToken)).StatusCode);

        var paid = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, $"/api/public/portal/payments/{paymentId}", session), CancellationToken));
        Assert.Equal("Paid", paid.GetProperty("status").GetString());
        await EventuallyAsync(async () => (await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, "/api/public/portal/order", session), CancellationToken)))
            .GetProperty("documents").EnumerateArray().Any(document => document.GetProperty("kind").GetString() == "Receipt"), "receipt issued by the outbox");
        var final = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, "/api/public/portal/order", session), CancellationToken));
        Assert.Equal("Paid", final.GetProperty("payment").GetProperty("status").GetString());
        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/payments/card", session), CancellationToken), HttpStatusCode.UnprocessableEntity, "portal.nothingToPay");
        var documents = final.GetProperty("documents").EnumerateArray().ToList();
        Assert.Equal(["Quote", "Receipt", "ServiceOrder"], documents.Select(document => document.GetProperty("kind").GetString()!).Order(StringComparer.Ordinal).ToArray());
        var receiptCode = documents.Single(document => document.GetProperty("kind").GetString() == "Receipt").GetProperty("code").GetString()!;

        var receipt = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, $"/api/public/portal/documents/{receiptCode}", session), CancellationToken));
        Assert.Equal(120m, receipt.GetProperty("total").GetDecimal());
        Assert.NotEqual(JsonValueKind.Null, receipt.GetProperty("paidAt").ValueKind);
        var portalPdf = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, $"/api/public/portal/documents/{receiptCode}/pdf", session), CancellationToken));
        Assert.StartsWith("data:application/pdf;base64,", portalPdf.GetProperty("url").GetString(), StringComparison.Ordinal);
        await AssertProblemAsync(await anonymous.GetAsync($"/api/public/portal/documents/{receiptCode}", CancellationToken), HttpStatusCode.Unauthorized, "portal.sessionExpired");
        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Get, "/api/public/portal/documents/OS-0-XXXXXX", session), CancellationToken), HttpStatusCode.NotFound, "documents.notFound");

        var verification = await ReadJsonAsync(await anonymous.GetAsync($"/api/public/documents/{receiptCode}/verification", CancellationToken));
        Assert.True(verification.GetProperty("valid").GetBoolean());
        Assert.Equal("Receipt", verification.GetProperty("kind").GetString());
        Assert.False((await ReadJsonAsync(await anonymous.GetAsync("/api/public/documents/NAO-EXISTE/verification", CancellationToken))).GetProperty("valid").GetBoolean());

        var adminDocument = await ReadJsonAsync(await admin.GetAsync($"/api/admin/documents/{receiptCode}", CancellationToken));
        Assert.Equal("Maria Souza", adminDocument.GetProperty("customerName").GetString());
        var adminPdf = await ReadJsonAsync(await admin.GetAsync($"/api/admin/documents/{receiptCode}/pdf", CancellationToken));
        Assert.Equal($"{receiptCode}.pdf", adminPdf.GetProperty("fileName").GetString());
        var email = await ReadJsonAsync(await admin.PostAsJsonAsync($"/api/admin/documents/{receiptCode}/send", new { channel = "Email" }, CancellationToken));
        Assert.Equal("Sent", email.GetProperty("status").GetString());
        Assert.Contains(Factory.Emails.Messages, message => message.To == "maria@exemplo.com");
        var whatsApp = await ReadJsonAsync(await admin.PostAsJsonAsync($"/api/admin/documents/{receiptCode}/send", new { channel = "WhatsApp" }, CancellationToken));
        Assert.Equal("Queued", whatsApp.GetProperty("status").GetString());
        await AssertProblemAsync(await admin.PostAsJsonAsync($"/api/admin/documents/{receiptCode}/send", new { channel = "Fax" }, CancellationToken), HttpStatusCode.BadRequest, "documents.invalidChannel");
        await AssertProblemAsync(await admin.GetAsync("/api/admin/documents/NAO-EXISTE", CancellationToken), HttpStatusCode.NotFound, "documents.notFound");

        var detail = await ReadJsonAsync(await admin.GetAsync($"/api/admin/service-orders/{orderId}", CancellationToken));
        Assert.Equal("InProgress", detail.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Portal_RejectsQuoteAndLocksAfterThreeWrongAttempts()
    {
        using var admin = await ClientAsync(Role.Admin);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var (_, quoteId) = await QuoteAsync(admin, mechanic);
        var token = await SendAsync(admin, quoteId);
        using var anonymous = Factory.CreateClient();
        var session = await ConfirmAsync(anonymous, token);

        await AssertProblemAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/quote/reject", session, new { reason = "x" }), CancellationToken), HttpStatusCode.BadRequest, "quotes.rejectReason");
        var rejected = await ReadJsonAsync(await anonymous.SendAsync(Portal(HttpMethod.Post, "/api/public/portal/quote/reject", session, new { reason = "Vou fazer depois" }), CancellationToken));
        Assert.Equal("Rejected", rejected.GetProperty("quote").GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, rejected.GetProperty("payment").ValueKind);
        var stored = await ReadJsonAsync(await admin.GetAsync($"/api/admin/quotes/{quoteId}", CancellationToken));
        Assert.Equal("Vou fazer depois", stored.GetProperty("rejectionReason").GetString());

        for (var attempt = 0; attempt < 2; attempt++)
        {
            Assert.Equal("WrongDigits", (await ReadJsonAsync(await anonymous.PostAsJsonAsync("/api/public/portal/confirm", new { token, lastDigits = "999" }, CancellationToken))).GetProperty("status").GetString());
        }

        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/public/portal/confirm", new { token, lastDigits = "999" }, CancellationToken), HttpStatusCode.Locked, "portal.locked");
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/public/portal/access", new { token }, CancellationToken), HttpStatusCode.Locked, "portal.locked");
        await AssertProblemAsync(await anonymous.PostAsJsonAsync("/api/public/portal/confirm", new { token, lastDigits = "321" }, CancellationToken), HttpStatusCode.Locked, "portal.locked");
    }
}
