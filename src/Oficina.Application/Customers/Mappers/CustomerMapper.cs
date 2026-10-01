using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace Oficina.Application.Customers.Mappers;

[Mapper]
public static partial class CustomerMapper
{
    [MapperIgnoreSource(nameof(Customer.DomainEvents))]
    [MapperIgnoreSource(nameof(Customer.DeletedAt))]
    [MapperIgnoreSource(nameof(Customer.IsDeleted))]
    public static partial CustomerDto ToDto(this Customer customer);

    public static Result<Email?> ToOptionalEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Email?>.Success(null);
        }

        var email = Email.Create(value);
        if (email.Error is { } error)
        {
            return error;
        }

        return email.Value;
    }

    public static Result<Address?> ToAddress(AddressInput? input) =>
        input is null
            ? Result<Address?>.Success(null)
            : Address.CreateOptional(input.ZipCode, input.Street, input.Number, input.Complement, input.District, input.City, input.State);

    private static partial AddressDto ToAddressDto(Address address);

    private static string DocumentToString(Document document) => document.Value;

    private static string EmailToString(Email email) => email.Value;

    private static string PhoneToString(Phone phone) => phone.Value;
}
