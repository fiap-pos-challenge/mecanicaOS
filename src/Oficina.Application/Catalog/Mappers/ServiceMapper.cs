using Oficina.Application.Catalog.Dtos;
using Oficina.Domain.Catalog.Entities;

namespace Oficina.Application.Catalog.Mappers;

public static class ServiceMapper
{
    public static ServiceDto ToDto(this Service service) => new(
        service.Id,
        service.Name,
        service.Description,
        service.BasePrice.Amount,
        service.EstimatedMinutes,
        service.CommissionPercent,
        service.WarrantyDays,
        service.UsualParts.Select(part => new UsualPartDto(part.PartId, part.Quantity.Value)).ToList(),
        service.IsActive,
        service.CreatedAt,
        service.UpdatedAt);
}
