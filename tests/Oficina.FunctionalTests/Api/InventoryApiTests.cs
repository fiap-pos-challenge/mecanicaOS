using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Oficina.Domain.Identity.Enums;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class InventoryApiTests(SqlServerFixture database) : ApiTestBase(database)
{
    private static async Task<Guid> PartAsync(HttpClient client, string code, string type = "Part", decimal minimum = 4m, string? barcode = null, string? supplier = null)
    {
        using var response = await client.PostAsJsonAsync("/api/admin/parts", new { code, name = $"Item {code}", type, unitOfMeasure = type == "Part" ? "un" : "l", unitPrice = 50m, cost = 20m, minimumStock = minimum, supplier, barcode }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task EntriesExitsOverviewAndMovements_TrackTheStock()
    {
        using var manager = await ClientAsync(Role.Manager);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var pads = await PartAsync(manager, "PST-001", barcode: "7891234567895");
        var oil = await PartAsync(manager, "OLE-001", "Supply", 2m);

        using var entry = await manager.PostAsJsonAsync("/api/admin/inventory/entries", new { partId = pads, quantity = 10m, unitCost = 22m, reason = "", supplierId = (Guid?)null, invoiceNumber = "4512" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, entry.StatusCode);
        var movement = await ReadJsonAsync(entry);
        Assert.Equal("In", movement.GetProperty("type").GetString());
        Assert.Equal("Nota 4512", movement.GetProperty("reason").GetString());
        Assert.Equal(10m, movement.GetProperty("balanceAfter").GetDecimal());
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/entries", new { partId = pads, quantity = 1.5m, unitCost = 22m }, CancellationToken), HttpStatusCode.BadRequest, "inventory.fractionalPart");
        Assert.Equal(HttpStatusCode.Created, (await manager.PostAsJsonAsync("/api/admin/inventory/entries", new { partId = oil, quantity = 1.25m, unitCost = 30m }, CancellationToken)).StatusCode);
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/entries", new { partId = Guid.CreateVersion7(), quantity = 1m, unitCost = 1m }, CancellationToken), HttpStatusCode.BadRequest, "inventory.partNotFound");

        using var exit = await manager.PostAsJsonAsync("/api/admin/inventory/exits", new { partId = pads, quantity = 8m, reason = "Aplicada", serviceOrderNumber = 3021 }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, exit.StatusCode);
        Assert.Equal(3021, (await ReadJsonAsync(exit)).GetProperty("serviceOrderNumber").GetInt64());
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/exits", new { partId = pads, quantity = 50m }, CancellationToken), HttpStatusCode.Conflict, "inventory.insufficientStock");
        Assert.Equal(HttpStatusCode.Forbidden, (await mechanic.PostAsJsonAsync("/api/admin/inventory/exits", new { partId = pads, quantity = 1m }, CancellationToken)).StatusCode);

        var overview = await ReadJsonAsync(await mechanic.GetAsync("/api/admin/inventory/stock?page=1&pageSize=10", CancellationToken));
        Assert.Equal(2, overview.GetProperty("totalItems").GetInt32());
        var kpis = overview.GetProperty("kpis");
        Assert.Equal(2, kpis.GetProperty("belowMinimum").GetInt32());
        Assert.Equal(8m, kpis.GetProperty("outflowMonth").GetDecimal());
        Assert.Equal("GERAL", Assert.Single(overview.GetProperty("locations").EnumerateArray()).GetString());
        var padsItem = overview.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("partId").GetGuid() == pads);
        Assert.Equal("Reorder", padsItem.GetProperty("situation").GetString());
        Assert.Equal(22m, padsItem.GetProperty("unitCost").GetDecimal());
        Assert.Equal(1, (await ReadJsonAsync(await mechanic.GetAsync("/api/admin/inventory/stock?situation=Reorder&term=pst", CancellationToken))).GetProperty("totalItems").GetInt32());

        var history = await ReadJsonAsync(await mechanic.GetAsync($"/api/admin/inventory/stock/{pads}/movements", CancellationToken));
        Assert.Equal(2, history.GetProperty("items").GetArrayLength());
        Assert.Equal("Out", history.GetProperty("items")[0].GetProperty("type").GetString());
        Assert.Equal("Manager User", history.GetProperty("items")[0].GetProperty("userName").GetString());
        await AssertProblemAsync(await mechanic.GetAsync($"/api/admin/inventory/stock/{Guid.CreateVersion7()}/movements", CancellationToken), HttpStatusCode.NotFound, "inventory.partNotFound");

        Assert.Equal(pads, (await ReadJsonAsync(await mechanic.PostAsJsonAsync("/api/admin/inventory/barcodes/lookup", new { barcode = "789 1234 567895" }, CancellationToken))).GetProperty("partId").GetGuid());
        await AssertProblemAsync(await mechanic.PostAsJsonAsync("/api/admin/inventory/barcodes/lookup", new { barcode = "0000000000000" }, CancellationToken), HttpStatusCode.NotFound, "inventory.unknownBarcode");
    }

    [Fact]
    public async Task Invoice_ReadsTheNfeXmlAndConfirmsTheEntries()
    {
        using var manager = await ClientAsync(Role.Manager);
        var pads = await PartAsync(manager, "PST-002", barcode: "7891234567895");
        var supplier = await ReadJsonAsync(await manager.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Distribuidora Freio Sul", document = "11.222.333/0001-81", email = "vendas@freiosul.example.com", phone = "(51) 3333-4444", leadTimeDays = 3 }, CancellationToken));
        const string xml = """
            <nfeProc xmlns="http://www.portalfiscal.inf.br/nfe"><NFe><infNFe>
            <ide><nNF>4588</nNF><dhEmi>2026-10-10T10:00:00-03:00</dhEmi></ide>
            <emit><CNPJ>11222333000181</CNPJ><xNome>FREIO SUL LTDA</xNome></emit>
            <det nItem="1"><prod><cEAN>7891234567895</cEAN><xProd>PASTILHA FREIO DIANT</xProd><qCom>2.0000</qCom><vUnCom>205.00</vUnCom></prod></det>
            <det nItem="2"><prod><cEAN>SEM GTIN</cEAN><xProd>FLUIDO DOT4</xProd><qCom>6</qCom><vUnCom>21.50</vUnCom></prod></det>
            </infNFe></NFe></nfeProc>
            """;

        using var read = await manager.PostAsJsonAsync("/api/admin/inventory/invoices/read", new { fileName = "nota.xml", contentType = "text/xml", content = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml)) }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        var draft = await ReadJsonAsync(read);
        Assert.Equal("4588", draft.GetProperty("invoiceNumber").GetString());
        Assert.Equal(supplier.GetProperty("id").GetGuid(), draft.GetProperty("supplierId").GetGuid());
        Assert.Equal("Distribuidora Freio Sul", draft.GetProperty("supplierName").GetString());
        var lines = draft.GetProperty("lines").EnumerateArray().ToArray();
        Assert.Equal(pads, lines[0].GetProperty("partId").GetGuid());
        Assert.Equal(JsonValueKind.Null, lines[1].GetProperty("partId").ValueKind);
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/invoices/read", new { fileName = "nota.pdf", contentType = "application/pdf", content = "JVBER" }, CancellationToken), HttpStatusCode.BadRequest, "inventory.invalidInvoiceFile");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/invoices/read", new { fileName = "nota.xml", contentType = "text/xml", content = "não é base64" }, CancellationToken), HttpStatusCode.BadRequest, "inventory.invalidInvoiceFile");

        using var confirmed = await manager.PostAsJsonAsync("/api/admin/inventory/invoices/confirm", new { supplierId = supplier.GetProperty("id").GetGuid(), invoiceNumber = "4588", lines = new[] { new { partId = pads, quantity = 2m, unitCost = 205m } } }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
        Assert.Equal(1, (await ReadJsonAsync(confirmed)).GetProperty("lines").GetInt32());
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/invoices/confirm", new { invoiceNumber = "4588", lines = Array.Empty<object>() }, CancellationToken), HttpStatusCode.BadRequest, "inventory.invalidInvoiceLines");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/invoices/confirm", new { invoiceNumber = "4588", lines = new[] { new { partId = pads, quantity = 0.5m, unitCost = 1m } } }, CancellationToken), HttpStatusCode.BadRequest, "inventory.invalidInvoiceLines");
    }

    [Fact]
    public async Task Count_OpenRecordSubmitApproveAndReject()
    {
        using var manager = await ClientAsync(Role.Manager);
        using var mechanic = await ClientAsync(Role.Mechanic);
        var pads = await PartAsync(manager, "PST-003");
        await manager.PostAsJsonAsync("/api/admin/inventory/entries", new { partId = pads, quantity = 5m, unitCost = 20m }, CancellationToken);

        using var opened = await mechanic.PostAsJsonAsync("/api/admin/inventory/counts", new { location = "geral" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, opened.StatusCode);
        var count = await ReadJsonAsync(opened);
        var id = count.GetProperty("id").GetGuid();
        Assert.Equal(5m, count.GetProperty("lines")[0].GetProperty("systemQuantity").GetDecimal());
        await AssertProblemAsync(await mechanic.PostAsJsonAsync("/api/admin/inventory/counts", new { location = "GERAL" }, CancellationToken), HttpStatusCode.Conflict, "inventory.countAlreadyOpen");
        await AssertProblemAsync(await mechanic.PostAsJsonAsync("/api/admin/inventory/counts", new { location = "Z9" }, CancellationToken), HttpStatusCode.BadRequest, "inventory.unknownLocation");
        await AssertProblemAsync(await mechanic.PostAsync($"/api/admin/inventory/counts/{id}/submit", null, CancellationToken), HttpStatusCode.UnprocessableEntity, "inventory.countIncomplete");
        Assert.Equal(HttpStatusCode.BadRequest, (await mechanic.PutAsJsonAsync($"/api/admin/inventory/counts/{id}/lines", new { lines = new[] { new { partId = pads, countedQuantity = (decimal?)2.5m } } }, CancellationToken)).StatusCode);
        var recorded = await ReadJsonAsync(await mechanic.PutAsJsonAsync($"/api/admin/inventory/counts/{id}/lines", new { lines = new[] { new { partId = pads, countedQuantity = (decimal?)3m } } }, CancellationToken));
        Assert.Equal(3m, recorded.GetProperty("lines")[0].GetProperty("countedQuantity").GetDecimal());
        Assert.Equal("PendingApproval", (await ReadJsonAsync(await mechanic.PostAsync($"/api/admin/inventory/counts/{id}/submit", null, CancellationToken))).GetProperty("status").GetString());
        await AssertProblemAsync(await mechanic.PutAsJsonAsync($"/api/admin/inventory/counts/{id}/lines", new { lines = new[] { new { partId = pads, countedQuantity = (decimal?)4m } } }, CancellationToken), HttpStatusCode.Conflict, "inventory.countClosed");
        Assert.Equal(HttpStatusCode.Forbidden, (await mechanic.PostAsync($"/api/admin/inventory/counts/{id}/approve", null, CancellationToken)).StatusCode);

        var approved = await ReadJsonAsync(await manager.PostAsync($"/api/admin/inventory/counts/{id}/approve", null, CancellationToken));
        Assert.Equal("Approved", approved.GetProperty("status").GetString());
        Assert.Equal("Manager User", approved.GetProperty("decidedBy").GetString());
        var adjustment = (await ReadJsonAsync(await manager.GetAsync($"/api/admin/inventory/stock/{pads}/movements", CancellationToken))).GetProperty("items")[0];
        Assert.Equal("Adjustment", adjustment.GetProperty("type").GetString());
        Assert.Equal(-2m, adjustment.GetProperty("quantity").GetDecimal());
        await AssertProblemAsync(await manager.PostAsync($"/api/admin/inventory/counts/{id}/approve", null, CancellationToken), HttpStatusCode.Conflict, "inventory.countNotPending");

        var second = (await ReadJsonAsync(await mechanic.PostAsJsonAsync("/api/admin/inventory/counts", new { location = "GERAL" }, CancellationToken))).GetProperty("id").GetGuid();
        await mechanic.PutAsJsonAsync($"/api/admin/inventory/counts/{second}/lines", new { lines = new[] { new { partId = pads, countedQuantity = (decimal?)3m } } }, CancellationToken);
        await mechanic.PostAsync($"/api/admin/inventory/counts/{second}/submit", null, CancellationToken);
        await AssertProblemAsync(await manager.PostAsJsonAsync($"/api/admin/inventory/counts/{second}/reject", new { reason = " " }, CancellationToken), HttpStatusCode.BadRequest, "common.required");
        var rejected = await ReadJsonAsync(await manager.PostAsJsonAsync($"/api/admin/inventory/counts/{second}/reject", new { reason = "Recontar prateleira" }, CancellationToken));
        Assert.Equal("Rejected", rejected.GetProperty("status").GetString());
        Assert.Equal(2, (await ReadJsonAsync(await mechanic.GetAsync("/api/admin/inventory/counts", CancellationToken))).GetProperty("items").GetArrayLength());
        Assert.Equal(id, (await ReadJsonAsync(await mechanic.GetAsync($"/api/admin/inventory/counts/{id}", CancellationToken))).GetProperty("id").GetGuid());
        await AssertProblemAsync(await mechanic.GetAsync($"/api/admin/inventory/counts/{Guid.CreateVersion7()}", CancellationToken), HttpStatusCode.NotFound, "inventory.countNotFound");
    }

    [Fact]
    public async Task SuppliersSuggestionsAndPurchaseOrders()
    {
        using var manager = await ClientAsync(Role.Manager);
        await PartAsync(manager, "PST-004", minimum: 4m, supplier: "Auto Peças Centro");

        using var created = await manager.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Auto Peças Centro", document = (string?)null, email = "pedidos@autopecas.example.com", phone = (string?)null, leadTimeDays = 2 }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var supplierId = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "X", leadTimeDays = 2 }, CancellationToken), HttpStatusCode.BadRequest, "inventory.invalidSupplierName");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Fornecedor", document = "52998224725", leadTimeDays = 2 }, CancellationToken), HttpStatusCode.BadRequest, "inventory.invalidSupplierDocument");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Fornecedor", email = "x@", leadTimeDays = 2 }, CancellationToken), HttpStatusCode.BadRequest, "common.invalidEmail");
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Fornecedor", leadTimeDays = 400 }, CancellationToken), HttpStatusCode.BadRequest, "inventory.invalidLeadTime");
        await manager.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Com CNPJ", document = "11222333000181", leadTimeDays = 1 }, CancellationToken);
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/suppliers", new { name = "Repetido", document = "11222333000181", leadTimeDays = 1 }, CancellationToken), HttpStatusCode.Conflict, "inventory.duplicateSupplier");
        var updated = await ReadJsonAsync(await manager.PutAsJsonAsync($"/api/admin/inventory/suppliers/{supplierId}", new { name = "Auto Peças Centro", email = "pedidos@autopecas.example.com", phone = "(11) 4004-1234", leadTimeDays = 5 }, CancellationToken));
        Assert.Equal("1140041234", updated.GetProperty("phone").GetString());
        await AssertProblemAsync(await manager.PutAsJsonAsync($"/api/admin/inventory/suppliers/{Guid.CreateVersion7()}", new { name = "Nada", leadTimeDays = 1 }, CancellationToken), HttpStatusCode.NotFound, "inventory.supplierNotFound");
        Assert.Equal(2, (await ReadJsonAsync(await manager.GetAsync("/api/admin/inventory/suppliers", CancellationToken))).GetProperty("items").GetArrayLength());

        var suggestion = Assert.Single((await ReadJsonAsync(await manager.GetAsync("/api/admin/inventory/purchase-orders/suggestions", CancellationToken))).GetProperty("items").EnumerateArray());
        Assert.Equal(8m, suggestion.GetProperty("suggestedQuantity").GetDecimal());
        Assert.Equal(supplierId, suggestion.GetProperty("supplierId").GetGuid());

        using var order = await manager.PostAsJsonAsync("/api/admin/inventory/purchase-orders", new { supplierId, lines = new[] { new { partId = suggestion.GetProperty("partId").GetGuid(), quantity = 8m, unitCost = 20m } } }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, order.StatusCode);
        var draft = await ReadJsonAsync(order);
        Assert.Equal(1001, draft.GetProperty("number").GetInt64());
        Assert.Equal(160m, draft.GetProperty("total").GetDecimal());
        Assert.Equal("Draft", draft.GetProperty("status").GetString());
        await AssertProblemAsync(await manager.PostAsJsonAsync("/api/admin/inventory/purchase-orders", new { supplierId = Guid.CreateVersion7(), lines = new[] { new { partId = suggestion.GetProperty("partId").GetGuid(), quantity = 1m, unitCost = 1m } } }, CancellationToken), HttpStatusCode.BadRequest, "inventory.invalidOrder");

        using var sent = await manager.PostAsync($"/api/admin/inventory/purchase-orders/{draft.GetProperty("id").GetGuid()}/send", null, CancellationToken);
        Assert.Equal(HttpStatusCode.Accepted, sent.StatusCode);
        Assert.Equal("Sent", (await ReadJsonAsync(sent)).GetProperty("status").GetString());
        Assert.Contains(Factory.Emails.Messages, message => message.To == "pedidos@autopecas.example.com" && message.Subject.Contains("1001", StringComparison.Ordinal));
        await AssertProblemAsync(await manager.PostAsync($"/api/admin/inventory/purchase-orders/{draft.GetProperty("id").GetGuid()}/send", null, CancellationToken), HttpStatusCode.Conflict, "inventory.orderAlreadySent");
        await AssertProblemAsync(await manager.PostAsync($"/api/admin/inventory/purchase-orders/{Guid.CreateVersion7()}/send", null, CancellationToken), HttpStatusCode.NotFound, "inventory.orderNotFound");
        Assert.Single((await ReadJsonAsync(await manager.GetAsync("/api/admin/inventory/purchase-orders", CancellationToken))).GetProperty("items").EnumerateArray());
    }
}
