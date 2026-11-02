using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Common.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace Oficina.Application.Catalog.Mappers;

[Mapper]
public static partial class ServiceMapper
{
    [MapperIgnoreSource(nameof(Service.DomainEvents))]
    [MapperIgnoreSource(nameof(Service.DeletedAt))]
    [MapperIgnoreSource(nameof(Service.IsDeleted))]
    public static partial ServiceDto ToDto(this Service service);

    private static decimal MoneyToDecimal(Money money) => money.Amount;
}
