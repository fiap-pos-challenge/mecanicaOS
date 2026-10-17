using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Catalog;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Catalog.Commands.CreatePackage;
using Oficina.Application.Catalog.Commands.DeactivatePackage;
using Oficina.Application.Catalog.Commands.ReactivatePackage;
using Oficina.Application.Catalog.Commands.UpdatePackage;
using Oficina.Application.Catalog.Dtos;
using Oficina.Application.Catalog.Queries.GetPackageById;
using Oficina.Application.Catalog.Queries.GetPackagePriceHistory;
using Oficina.Application.Catalog.Queries.ListPackages;
using Oficina.Application.Common.Pagination;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/packages")]
[Tags("Packages")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class PackagesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("List packages")]
    [EndpointDescription("Returns a page of packages ordered by name, with items, current item prices, items total and savings. The term filters by name. The status filters active or inactive packages; without status, only active packages are listed unless includeInactive is true. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PagedResult<PackageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? term = null,
        [FromQuery] CatalogStatus? status = null,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListPackagesQuery(page, pageSize, includeInactive, term, status), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Create package")]
    [EndpointDescription("Creates an active package with name, optional description, 1 to 50 active services or parts (no repetition, quantity greater than zero), price greater than zero and an optional mileage suggestion. The first price goes to the price history. Roles: Admin, Manager.")]
    [ProducesResponseType<PackageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(PackageRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePackageCommand(request.Name, request.Description, request.Price, Items(request.Items), request.Suggestion?.MinMileage, request.Suggestion?.MaxMileage);
        return Respond(await sender.Send(command, cancellationToken), package => CreatedAtAction(nameof(GetById), new { id = package.Id }, package));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get package")]
    [EndpointDescription("Returns one package, active or inactive, with items total and savings. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PackageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPackageByIdQuery(id), cancellationToken), Ok);

    [HttpGet("{id:guid}/price-history")]
    [Authorize(Policy = Policies.Authenticated)]
    [EndpointSummary("Get package price history")]
    [EndpointDescription("Returns the package price changes, newest first, with the author name. Roles: Admin, Manager, Attendant, Mechanic.")]
    [ProducesResponseType<PriceHistoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> PriceHistory(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetPackagePriceHistoryQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Update package")]
    [EndpointDescription("Replaces name, description, items, price and mileage suggestion of an active package. A new price goes to the price history. Inactive packages must be reactivated first. Roles: Admin, Manager.")]
    [ProducesResponseType<PackageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, PackageRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePackageCommand(id, request.Name, request.Description, request.Price, Items(request.Items), request.Suggestion?.MinMileage, request.Suggestion?.MaxMileage);
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Deactivate package")]
    [EndpointDescription("Hides the package from the default listing and blocks changes. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivatePackageCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Reactivate package")]
    [EndpointDescription("Returns an inactive package to the default listing and allows changes again. Roles: Admin, Manager.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivatePackageCommand(id), cancellationToken));

    private static IReadOnlyList<PackageItemRequest> Items(IReadOnlyList<PackageItemBody>? items) =>
        (items ?? []).Select(item => new PackageItemRequest(item.Kind, item.ItemId, item.Quantity)).ToList();
}
