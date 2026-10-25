using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Addresses;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Addresses.Dtos;
using Oficina.Application.Addresses.Queries.LookupAddress;

namespace Oficina.Api.Controllers.Admin;

[Route("api/addresses")]
[Tags("Addresses")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class AddressesController(ISender sender) : ApiControllerBase
{
    [HttpPost("lookup")]
    [Authorize(Policy = Policies.Staff)]
    [EndpointSummary("Look up address by zip code")]
    [EndpointDescription("Returns street, district, city and state of a Brazilian zip code (CEP) sent in the body. Returns 404 address.zipCodeNotFound when the zip code does not exist or the lookup service is unavailable. Roles: Admin, Manager, Attendant.")]
    [ProducesResponseType<AddressLookupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Lookup(LookupAddressRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new LookupAddressQuery(request.ZipCode), cancellationToken), Ok);
}
