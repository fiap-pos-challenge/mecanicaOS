namespace Oficina.Application.Customers.Dtos;

public sealed record AddressDto(
    string ZipCode,
    string Street,
    string Number,
    string? Complement,
    string District,
    string City,
    string State);
