using Oficina.Application.Customers.Dtos;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.UnitTests.Application;

internal static class CustomerTestData
{
    public const string Cpf = "52998224725";
    public const string MaskedCpf = "529.982.247-25";
    public const string Cnpj = "11222333000181";
    public const string PhoneNumber = "11987654321";
    public const string EmailAddress = "maria@exemplo.com";

    public static AddressInput AddressInput() => new("01310-100", "Av. Paulista", "1000", "Sala 1", "Bela Vista", "São Paulo", "sp");

    public static Customer Customer() =>
        Oficina.Domain.Customers.Entities.Customer.Create(
            "Maria Souza",
            Document.Create(Cpf).Value,
            Email.Create(EmailAddress).Value,
            Phone.Create(PhoneNumber).Value,
            null).Value;
}
