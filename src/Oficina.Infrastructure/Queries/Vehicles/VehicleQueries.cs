using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dto;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Application.Vehicles.Queries;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Vehicles;

public sealed class VehicleQueries(OficinaDbContext context) : IVehiclesQueries
{
    public async Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles.AsNoTracking().SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);
        return vehicle?.ToDto();
    }

    public async Task<VehicleDto?> GetByLicensePlate(LicensePlate licensePlate, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles.AsNoTracking().SingleOrDefaultAsync(vehicle => vehicle.LicensePlate == licensePlate, cancellationToken);
        return vehicle?.ToDto();
    }

    public async Task<PagedResult<VehicleDto>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var vehicles = context.Vehicles.AsNoTracking();
        var totalItems = await vehicles.CountAsync(cancellationToken);
        var items = await vehicles
            .OrderBy(vehicle => vehicle.Brand)
            .ThenBy(vehicle => vehicle.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<VehicleDto>(items.Select(customer => customer.ToDto()).ToList(), page.Page, page.PageSize, totalItems);
    }
}
