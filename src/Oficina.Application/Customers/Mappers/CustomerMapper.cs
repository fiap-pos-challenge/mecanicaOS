using Oficina.Application.Common.Privacy;
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
    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id,
        customer.Name,
        SensitiveDataMasker.MaskDocument(customer.Document),
        customer.DocumentType,
        customer.Email is null ? null : SensitiveDataMasker.MaskEmail(customer.Email),
        SensitiveDataMasker.MaskPhone(customer.Phone),
        customer.Address is null ? null : ToAddressDto(customer.Address),
        new MessagingConsentDto(customer.ConsentWhatsApp, customer.ConsentEmail, customer.ConsentUpdatedAt),
        customer.CreatedAt,
        customer.UpdatedAt);

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

}
