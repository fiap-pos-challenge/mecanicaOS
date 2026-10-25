using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Domain.ServiceOrders.Enums;

namespace Oficina.UnitTests.Application;

public static class ServiceOrderTestData
{
    public static ServiceOrderVehicleDto Vehicle() => new(Guid.CreateVersion7(), "AAA9999", "Fiat", "Uno", 2026);

    public static ServiceOrderDetailDto Detail(Guid id) => new(
        id,
        1001,
        ServiceOrderStatus.Received,
        0,
        "AAA9999",
        "Maria Souza",
        "Fiat Uno 2026",
        "Barulho ao frear",
        null,
        null,
        false,
        [],
        TimeProvider.System.GetUtcNow(),
        "Barulho ao frear",
        null,
        0m,
        new ServiceOrderCustomerDto(Guid.CreateVersion7(), "Maria Souza", CustomerTestData.Cpf, CustomerTestData.EmailAddress),
        Vehicle(),
        [],
        [],
        null,
        [],
        [],
        [],
        [],
        TimeProvider.System.GetUtcNow(),
        null,
        null,
        null);

    public static PublicServiceOrderDto Public() => new(
        1001,
        ServiceOrderStatus.Received,
        "Barulho ao frear",
        0m,
        Vehicle(),
        new List<ServiceOrderServiceItemDto>(),
        new List<ServiceOrderPartItemDto>(),
        null,
        TimeProvider.System.GetUtcNow(),
        null,
        null,
        null);
}
