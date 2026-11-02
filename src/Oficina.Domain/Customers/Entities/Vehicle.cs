using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Domain.Customers.Entities;

public sealed class Vehicle : AggregateRoot
{
    public const int BrandMaxLength = 60;
    public const int ModelMaxLength = 60;

    private Vehicle(Guid id, Guid customerId, LicensePlate licensePlate, string brand, string model, VehicleYear year)
        : base(id)
    {
        CustomerId = customerId;
        LicensePlate = licensePlate;
        Brand = brand;
        Model = model;
        Year = year;
    }

    public Guid CustomerId { get; private set; }

    public LicensePlate LicensePlate { get; private set; }

    public string Brand { get; private set; }

    public string Model { get; private set; }

    public VehicleYear Year { get; private set; }

    public static Result<Vehicle> Create(
        Guid customerId,
        LicensePlate licensePlate,
        string brand,
        string model,
        VehicleYear year,
        DateTimeOffset today)
    {
        if (customerId == Guid.Empty)
        {
            return VehicleErrors.CustomerRequired;
        }

        if (FindDetailsError(brand, model, year, today) is { } error)
        {
            return error;
        }

        return new Vehicle(Guid.CreateVersion7(), customerId, licensePlate, brand.Trim(), model.Trim(), year);
    }

    public Result ChangeDetails(string brand, string model, VehicleYear year, DateTimeOffset today)
    {
        if (FindDetailsError(brand, model, year, today) is { } error)
        {
            return error;
        }

        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        return Result.Success();
    }

    private static Error? FindDetailsError(string brand, string model, VehicleYear year, DateTimeOffset today)
    {
        if (string.IsNullOrWhiteSpace(brand) || brand.Trim().Length > BrandMaxLength)
        {
            return VehicleErrors.InvalidBrand;
        }

        if (string.IsNullOrWhiteSpace(model) || model.Trim().Length > ModelMaxLength)
        {
            return VehicleErrors.InvalidModel;
        }

        if (year.IsAfterNextYear(today))
        {
            return VehicleErrors.InvalidYear;
        }

        return null;
    }
}
