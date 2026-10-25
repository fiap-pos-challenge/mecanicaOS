namespace Oficina.Application.Addresses.Dtos;

public sealed record AddressLookupDto(
    string ZipCode,
    string Street,
    string? Number,
    string? Complement,
    string District,
    string City,
    string State);
