using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Domain.Customers.Entities;

public sealed class Vehicle : AggregateRoot
{
    public const int BrandMaxLength = 60;
    public const int ModelMaxLength = 60;
    public const int MaxMileage = 9_999_999;

    private Vehicle(Guid id, Guid customerId, LicensePlate licensePlate, string brand, string model, VehicleYear year, int mileage)
        : base(id)
    {
        CustomerId = customerId;
        LicensePlate = licensePlate;
        Brand = brand;
        Model = model;
        Year = year;
        Mileage = mileage;
    }

    public Guid CustomerId { get; private set; }

    public LicensePlate LicensePlate { get; private set; }

    public string Brand { get; private set; }

    public string Model { get; private set; }

    public VehicleYear Year { get; private set; }

    public int Mileage { get; private set; }

    public static Result<Vehicle> Create(
        Guid customerId,
        LicensePlate licensePlate,
        string brand,
        string model,
        VehicleYear year,
        int mileage,
        DateTimeOffset today)
    {
        if (customerId == Guid.Empty)
        {
            return VehicleErrors.CustomerRequired;
        }

        if (FindDetailsError(brand, model, year, mileage, today) is { } error)
        {
            return error;
        }

        return new Vehicle(Guid.CreateVersion7(), customerId, licensePlate, brand.Trim(), model.Trim(), year, mileage);
    }

    public Result ChangeDetails(string brand, string model, VehicleYear year, int mileage, DateTimeOffset today)
    {
        if (FindDetailsError(brand, model, year, mileage, today) is { } error)
        {
            return error;
        }

        if (mileage < Mileage)
        {
            return VehicleErrors.MileageDecrease;
        }

        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        Mileage = mileage;
        return Result.Success();
    }

    public Result TransferTo(Guid newCustomerId)
    {
        if (newCustomerId == Guid.Empty)
        {
            return VehicleErrors.CustomerRequired;
        }

        if (newCustomerId == CustomerId)
        {
            return VehicleErrors.SameOwner;
        }

        CustomerId = newCustomerId;
        return Result.Success();
    }

    private static Error? FindDetailsError(string brand, string model, VehicleYear year, int mileage, DateTimeOffset today)
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

        if (mileage is < 0 or > MaxMileage)
        {
            return VehicleErrors.InvalidMileage;
        }

        return null;
    }
}
