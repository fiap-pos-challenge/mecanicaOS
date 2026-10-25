using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Inventory;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Inventory.Commands.ConfirmInvoice;
using Oficina.Application.Inventory.Commands.CreatePurchaseOrder;
using Oficina.Application.Inventory.Commands.CreateSupplier;
using Oficina.Application.Inventory.Commands.DecideCount;
using Oficina.Application.Inventory.Commands.OpenCount;
using Oficina.Application.Inventory.Commands.RecordCount;
using Oficina.Application.Inventory.Commands.RegisterEntry;
using Oficina.Application.Inventory.Commands.RegisterExit;
using Oficina.Application.Inventory.Commands.SendPurchaseOrder;
using Oficina.Application.Inventory.Commands.UpdateSupplier;
using Oficina.Application.Inventory.Dtos;
using Oficina.Application.Inventory.Queries.GetCount;
using Oficina.Application.Inventory.Queries.GetMovements;
using Oficina.Application.Inventory.Queries.GetStockOverview;
using Oficina.Application.Inventory.Queries.ListCounts;
using Oficina.Application.Inventory.Queries.ListPurchaseOrders;
using Oficina.Application.Inventory.Queries.ListSuggestions;
using Oficina.Application.Inventory.Queries.ListSuppliers;
using Oficina.Application.Inventory.Queries.LookupBarcode;
using Oficina.Application.Inventory.Queries.ReadInvoice;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/inventory")]
[Tags("Inventory")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class InventoryController(ISender sender) : ApiControllerBase
{
    [HttpGet("stock")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Stock overview")]
    [EndpointDescription("Returns indicators and a page of active parts with location, stock, minimum, outflow of the last 30 days, unit cost, last outflow and situation (Ok, Reorder, OutOfStock or Idle after 90 days without outflow). Filters: term (name or code), situation and location. Roles: all workshop roles.")]
    [ProducesResponseType<StockOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Stock([FromQuery] int page = PageRequest.FirstPage, [FromQuery] int pageSize = PageRequest.DefaultPageSize, [FromQuery] string? term = null, [FromQuery] string? situation = null, [FromQuery] string? location = null, CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new GetStockOverviewQuery(page, pageSize, term, situation, location), cancellationToken), Ok);

    [HttpGet("stock/{partId:guid}/movements")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Stock movements")]
    [EndpointDescription("Returns the stock item and its movements (In, Out, Adjustment) from the newest, with the user name and the service order number. Roles: all workshop roles.")]
    [ProducesResponseType<StockMovementListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Movements(Guid partId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetMovementsQuery(partId), cancellationToken), Ok);

    [HttpPost("entries")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Register stock entry")]
    [EndpointDescription("Adds stock to a part with unit cost (the part cost is updated), reason, supplier and invoice number. Parts take whole units; supplies accept 3 decimals. Roles: Admin, Manager.")]
    [ProducesResponseType<StockMovementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Entry(StockEntryRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RegisterEntryCommand(request.PartId, request.Quantity, request.UnitCost, request.Reason, request.SupplierId, request.InvoiceNumber), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpPost("exits")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Register stock exit")]
    [EndpointDescription("Removes stock from a part with reason and optional service order number. Not enough stock: 422 inventory.insufficientStock. Roles: Admin, Manager.")]
    [ProducesResponseType<StockMovementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Exit(StockExitRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RegisterExitCommand(request.PartId, request.Quantity, request.Reason, request.ServiceOrderNumber), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpPost("barcodes/lookup")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Find part by barcode")]
    [EndpointDescription("Returns the stock item of the EAN-13 barcode sent in the body. Unknown: 404 inventory.unknownBarcode. Roles: all workshop roles.")]
    [ProducesResponseType<StockItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Barcode(BarcodeRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new LookupBarcodeQuery(request.Barcode ?? string.Empty), cancellationToken), Ok);

    [HttpPost("invoices/read")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Read invoice")]
    [EndpointDescription("Reads the NF-e XML sent in base64 and returns supplier, number, date and lines, linked to parts by barcode and to the supplier by CNPJ. PDF or invalid file: 400 inventory.invalidInvoiceFile. Roles: Admin, Manager.")]
    [ProducesResponseType<InvoiceDraftDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> ReadInvoice(InvoiceFileRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReadInvoiceQuery(request.FileName ?? string.Empty, request.ContentType ?? string.Empty, request.Content ?? string.Empty), cancellationToken), Ok);

    [HttpPost("invoices/confirm")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Confirm invoice")]
    [EndpointDescription("Registers one stock entry per invoice line with its unit cost. Roles: Admin, Manager.")]
    [ProducesResponseType<InvoiceConfirmedDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> ConfirmInvoice(InvoiceConfirmRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ConfirmInvoiceCommand(request.SupplierId, request.InvoiceNumber ?? string.Empty, request.Lines ?? []), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpGet("counts")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List stock counts")]
    [EndpointDescription("Returns the stock counts from the newest. Roles: all workshop roles.")]
    [ProducesResponseType<StockCountListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Counts(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListCountsQuery(), cancellationToken), Ok);

    [HttpPost("counts")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Open stock count")]
    [EndpointDescription("Opens a count of the active parts of a location with the current system quantities. One open count per location: 409 inventory.countAlreadyOpen. Roles: all workshop roles.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> OpenCount(OpenCountRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new OpenCountCommand(request.Location ?? string.Empty), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpGet("counts/{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get stock count")]
    [EndpointDescription("Returns one stock count with its lines. Roles: all workshop roles.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Count(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetCountQuery(id), cancellationToken), Ok);

    [HttpPut("counts/{id:guid}/lines")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Record counted quantities")]
    [EndpointDescription("Saves the counted quantity of lines of an open count (null clears). Roles: all workshop roles.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> CountLines(Guid id, CountLinesRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new RecordCountCommand(id, request.Lines ?? []), cancellationToken), Ok);

    [HttpPost("counts/{id:guid}/submit")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Submit stock count")]
    [EndpointDescription("Sends a fully counted count for approval. Missing lines: 422 inventory.countIncomplete. Roles: all workshop roles.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> SubmitCount(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DecideCountCommand(id, "submit", null), cancellationToken), Ok);

    [HttpPost("counts/{id:guid}/approve")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Approve stock count")]
    [EndpointDescription("Approves a submitted count and records one adjustment movement per difference. Roles: Admin, Manager.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> ApproveCount(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DecideCountCommand(id, "approve", null), cancellationToken), Ok);

    [HttpPost("counts/{id:guid}/reject")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Reject stock count")]
    [EndpointDescription("Rejects a submitted count with a reason; stock does not change. Roles: Admin, Manager.")]
    [ProducesResponseType<StockCountDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> RejectCount(Guid id, CountDecisionRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DecideCountCommand(id, "reject", request.Reason), cancellationToken), Ok);

    [HttpGet("suppliers")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List suppliers")]
    [EndpointDescription("Returns the suppliers by name. Roles: all workshop roles.")]
    [ProducesResponseType<SupplierListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Suppliers(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListSuppliersQuery(), cancellationToken), Ok);

    [HttpPost("suppliers")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Create supplier")]
    [EndpointDescription("Creates a supplier with name, optional CNPJ (unique), e-mail and phone and the lead time in days (0 to 365). Roles: Admin, Manager.")]
    [ProducesResponseType<SupplierDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> CreateSupplier(SupplierRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CreateSupplierCommand(request.Name ?? string.Empty, request.Document, request.Email, request.Phone, request.LeadTimeDays), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpPut("suppliers/{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Update supplier")]
    [EndpointDescription("Updates the supplier data. Roles: Admin, Manager.")]
    [ProducesResponseType<SupplierDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> UpdateSupplier(Guid id, SupplierRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new UpdateSupplierCommand(id, request.Name ?? string.Empty, request.Document, request.Email, request.Phone, request.LeadTimeDays), cancellationToken), Ok);

    [HttpGet("purchase-orders/suggestions")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Purchase suggestions")]
    [EndpointDescription("Suggests quantities for parts below the minimum: up to the larger of twice the minimum or the 30-day outflow plus the minimum. Roles: Admin, Manager.")]
    [ProducesResponseType<PurchaseSuggestionListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Suggestions(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListSuggestionsQuery(), cancellationToken), Ok);

    [HttpGet("purchase-orders")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("List purchase orders")]
    [EndpointDescription("Returns the purchase orders from the newest. Roles: Admin, Manager.")]
    [ProducesResponseType<PurchaseOrderListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Orders(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListPurchaseOrdersQuery(), cancellationToken), Ok);

    [HttpPost("purchase-orders")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Create purchase order")]
    [EndpointDescription("Creates a draft purchase order for a supplier with part lines (quantity and unit cost). Roles: Admin, Manager.")]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> CreateOrder(PurchaseOrderRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new CreatePurchaseOrderCommand(request.SupplierId, request.Lines ?? []), cancellationToken), result => StatusCode(StatusCodes.Status201Created, result));

    [HttpPost("purchase-orders/{id:guid}/send")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Send purchase order")]
    [EndpointDescription("Queues a draft order and e-mails it to the supplier; Sent when the e-mail goes out. Already sent: 409 inventory.orderAlreadySent. Roles: Admin, Manager.")]
    [ProducesResponseType<PurchaseOrderDto>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> SendOrder(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SendPurchaseOrderCommand(id), cancellationToken), result => Accepted(result));
}
