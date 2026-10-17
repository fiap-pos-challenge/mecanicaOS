using Oficina.Application.Addresses.Dtos;

namespace Oficina.Application.Abstractions.Addresses;

public interface IAddressLookup
{
    Task<AddressLookupDto?> FindAsync(string zipCode, CancellationToken cancellationToken);
}
