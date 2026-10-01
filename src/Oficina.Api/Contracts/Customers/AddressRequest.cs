using Oficina.Application.Customers.Dtos;

namespace Oficina.Api.Contracts.Customers;

public sealed record AddressRequest(
    string? ZipCode,
    string? Street,
    string? Number,
    string? Complement,
    string? District,
    string? City,
    string? State)
{
    public AddressInput ToInput() => new(ZipCode, Street, Number, Complement, District, City, State);
}
