using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Customers.Errors;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Vehicles.Validation;

public static class VehicleRules
{
    public static IRuleBuilderOptionsConditions<T, string> ValidPlate<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithError(VehicleErrors.InvalidLicensePlate).MustCreate(LicensePlate.Create);

    public static IRuleBuilderOptionsConditions<T, int> ValidYear<T>(this IRuleBuilderInitial<T, int> rule) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithError(VehicleErrors.InvalidYear).MustCreate(VehicleYear.Create);

    public static IRuleBuilderOptions<T, string> ValidBrand<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Must(brand => brand.Trim().Length > 0).WithError(VehicleErrors.InvalidBrand);

    public static IRuleBuilderOptions<T, string> ValidModel<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Must(model => model.Trim().Length > 0).WithError(VehicleErrors.InvalidModel);
}
