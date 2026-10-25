using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.ValueObjects;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.ValueObjects;

namespace Oficina.FunctionalTests.Fixtures;

public static class TestData
{
    public static readonly DateTimeOffset Today = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    public static Customer Customer(string document = "52998224725", Address? address = null) =>
        Domain.Customers.Entities.Customer.Create(
            "Maria Souza",
            Document.Create(document).Value,
            Email.Create("maria@exemplo.com").Value,
            Phone.Create("11987654321").Value,
            address).Value;

    public static Address FullAddress() =>
        Address.CreateOptional("01310100", "Avenida Paulista", "1000", "Sala 10", "Bela Vista", "São Paulo", "SP").Value
        ?? throw new InvalidOperationException(nameof(FullAddress));

    public static Vehicle Vehicle(Guid customerId, string plate = "BRA2E19") =>
        Domain.Customers.Entities.Vehicle.Create(
            customerId,
            LicensePlate.Create(plate).Value,
            "Volkswagen",
            "Gol",
            VehicleYear.Create(2020).Value, 42_000, Today).Value;

    public static User User(string email = "admin@oficina.com", Role role = Role.Admin) =>
        Domain.Identity.Entities.User.Create("Ana Lima", Email.Create(email).Value, "$2a$12$hash", role).Value;

    public static Service Service(decimal price = 100m) =>
        Domain.Catalog.Entities.Service.Create("Troca de óleo", "Inclui filtro", Money.Create(price).Value, 30, 10m, 90).Value;

    public static Part Part(string code = "FLT-001", decimal stock = 10m)
    {
        var part = Domain.Inventory.Entities.Part.Create(
            PartCode.Create(code).Value,
            "Filtro de óleo",
            PartType.Part,
            "un",
            Money.Create(25m).Value, Money.Zero,
            2m, null, null).Value;
        if (stock > 0)
        {
            part.RegisterEntry(Quantity.Create(stock).Value, "Carga inicial", null);
        }

        return part;
    }

    public static ServiceOrder ServiceOrder(Guid customerId, Guid vehicleId, long number) =>
        Domain.ServiceOrders.Entities.ServiceOrder.Open(
            ServiceOrderNumber.Create(number).Value,
            customerId,
            vehicleId,
            "Barulho ao frear",
            null,
            Today).Value;

    public static ApprovalToken Token(char character = 'a') => ApprovalToken.Create(new string(character, 64)).Value;
}
