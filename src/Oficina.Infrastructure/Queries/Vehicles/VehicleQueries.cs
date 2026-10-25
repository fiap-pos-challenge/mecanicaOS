using Microsoft.EntityFrameworkCore;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Vehicles.Dtos;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Application.Vehicles.Queries;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Infrastructure.Persistence;

namespace Oficina.Infrastructure.Queries.Vehicles;

public sealed class VehicleQueries(OficinaDbContext context) : IVehicleQueries
{
    public async Task<VehicleDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles.AsNoTracking().SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);
        if (vehicle is null)
        {
            return null;
        }

        var ownerName = await context.Customers.AsNoTracking()
            .Where(customer => customer.Id == vehicle.CustomerId)
            .Select(customer => customer.Name)
            .SingleOrDefaultAsync(cancellationToken);

        var serviceOrders = await context.ServiceOrders.AsNoTracking()
            .Where(order => order.VehicleId == id)
            .ToListAsync(cancellationToken);

        var history = serviceOrders
            .OrderByDescending(order => order.Number.Value)
            .Select(order => new VehicleServiceOrderSummaryDto(order.Number.Value, order.Status, order.CreatedAt, order.CompletedAt))
            .ToList();

        return new VehicleDetailDto(
            vehicle.Id,
            vehicle.Model,
            vehicle.Brand,
            vehicle.LicensePlate.Value,
            vehicle.Year.Value,
            vehicle.Mileage,
            new VehicleOwnerDto(vehicle.CustomerId, ownerName ?? string.Empty),
            history,
            vehicle.CreatedAt,
            vehicle.UpdatedAt);
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

        return new PagedResult<VehicleDto>(items.Select(vehicle => vehicle.ToDto()).ToList(), page.Page, page.PageSize, totalItems);
    }

    public async Task<IReadOnlyList<VehicleDto>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var vehicles = await context.Vehicles.AsNoTracking()
            .Where(vehicle => vehicle.CustomerId == customerId)
            .OrderBy(vehicle => vehicle.Brand)
            .ThenBy(vehicle => vehicle.Model)
            .ThenBy(vehicle => vehicle.Id)
            .ToListAsync(cancellationToken);
        return vehicles.Select(vehicle => vehicle.ToDto()).ToList();
    }

    public async Task<VehicleDto?> FindByPlateAsync(LicensePlate plate, CancellationToken cancellationToken)
    {
        var vehicle = await context.Vehicles.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.LicensePlate == plate, cancellationToken);
        return vehicle?.ToDto();
    }
}
