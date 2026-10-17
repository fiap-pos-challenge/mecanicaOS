using MediatR;
using Oficina.Application.Abstractions.Addresses;
using Oficina.Application.Addresses.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Application.Addresses.Queries.LookupAddress;

public sealed class LookupAddressQueryHandler(IAddressLookup lookup) : IRequestHandler<LookupAddressQuery, Result<AddressLookupDto>>
{
    public async Task<Result<AddressLookupDto>> Handle(LookupAddressQuery query, CancellationToken cancellationToken)
    {
        var zipCode = new string(query.ZipCode.Where(char.IsAsciiDigit).ToArray());
        return await lookup.FindAsync(zipCode, cancellationToken) is { } address ? address : CustomerErrors.ZipCodeNotFound;
    }
}
