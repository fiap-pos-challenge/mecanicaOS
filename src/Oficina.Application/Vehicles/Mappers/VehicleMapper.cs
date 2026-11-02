using Oficina.Application.Vehicles.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.ValueObjects;
using Riok.Mapperly.Abstractions;

namespace Oficina.Application.Vehicles.Mappers;

[Mapper]
public static partial class VehicleMapper
{
    [MapperIgnoreSource(nameof(Vehicle.DomainEvents))]
    [MapperIgnoreSource(nameof(Vehicle.IsDeleted))]
    [MapperIgnoreSource(nameof(Vehicle.DeletedAt))]
    public static partial VehicleDto ToDto(this Vehicle vehicle);

    public static Result<LicensePlate?> ToOptionalLicensePlate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<LicensePlate?>.Success(null);
        }

        var plate = LicensePlate.Create(value);
        if (plate.Error is { } error)
        {
            return error;
        }

        return plate.Value;
    }

    private static string LicensePlateToString(LicensePlate licensePlate) => licensePlate.Value;

    private static int VehicleYearToInt(VehicleYear vehicleYear) => vehicleYear.Value;
}
