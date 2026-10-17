using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Application.Vehicles.Mappers;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Vehicles.Validation;

public static class VehicleRules
{
    public static IRuleBuilderOptionsConditions<T, string> ValidPlate<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithError(VehicleErrors.InvalidLicensePlate).MustCreate(LicensePlate.Create);

    public static IRuleBuilderOptionsConditions<T, string?> ValidOptionalPlate<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MustCreate(VehicleMapper.ToOptionalLicensePlate);

    public static IRuleBuilderOptionsConditions<T, int> ValidYear<T>(this IRuleBuilderInitial<T, int> rule) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithError(VehicleErrors.InvalidYear).MustCreate(VehicleYear.Create);

    public static IRuleBuilderOptions<T, int> ValidMileage<T>(this IRuleBuilderInitial<T, int> rule) =>
        rule.InclusiveBetween(0, Vehicle.MaxMileage).WithError(VehicleErrors.InvalidMileage);

    public static IRuleBuilderOptions<T, string> ValidBrand<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Must(brand => brand.Trim().Length > 0).WithError(VehicleErrors.InvalidBrand);

    public static IRuleBuilderOptions<T, string> ValidModel<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Must(model => model.Trim().Length > 0).WithError(VehicleErrors.InvalidModel);
}
