using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Customers;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Customers.Commands.CreateCustomer;
using Oficina.Application.Customers.Commands.DeleteCustomer;
using Oficina.Application.Customers.Commands.UpdateCustomer;
using Oficina.Application.Customers.Dtos;
using Oficina.Application.Customers.Queries.GetCustomerById;
using Oficina.Application.Customers.Queries.ListCustomers;
using Oficina.Application.Customers.Queries.SearchCustomerByDocument;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/customers")]
[Tags("Customers")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class CustomersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("List customers")]
    [EndpointDescription("Returns a page of customers ordered by name. Roles: Admin, Attendant.")]
    [ProducesResponseType<PagedResult<CustomerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListCustomersQuery(page, pageSize), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Create customer")]
    [EndpointDescription("Creates a customer with CPF or CNPJ, phone and optional e-mail and address (all or nothing). Roles: Admin, Attendant.")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCustomerCommand(request.Name, request.Document, request.Email, request.Phone, request.Address?.ToInput());
        return Respond(await sender.Send(command, cancellationToken), customer => CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Get customer")]
    [EndpointDescription("Returns one customer by id. Roles: Admin, Attendant.")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetCustomerByIdQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Update customer")]
    [EndpointDescription("Updates name, contact and address. The document cannot change. Roles: Admin, Attendant.")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCustomerCommand(id, request.Name, request.Email, request.Phone, request.Address?.ToInput());
        return Respond(await sender.Send(command, cancellationToken), Ok);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Delete customer")]
    [EndpointDescription("Soft deletes a customer without open service orders or registered vehicles. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeleteCustomerCommand(id), cancellationToken));

    [HttpPost("search")]
    [Authorize(Policy = Policies.AdminOrAttendant)]
    [EndpointSummary("Search customer by document")]
    [EndpointDescription("Finds a customer by CPF or CNPJ sent in the body (never in the URL). Roles: Admin, Attendant.")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Search(SearchCustomerRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SearchCustomerByDocumentQuery(request.Document), cancellationToken), Ok);
}
