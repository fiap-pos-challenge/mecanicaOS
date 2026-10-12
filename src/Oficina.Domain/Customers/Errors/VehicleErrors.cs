using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Customers.Errors;

public static class VehicleErrors
{
    public static readonly Error CustomerRequired = new(
        "vehicle.customerRequired",
        "Informe o cliente dono do veículo.",
        ErrorType.Validation);

    public static readonly Error NotFound = new (
        "vehicle.notFound",
        "Veículo não encontrado.",
        ErrorType.Validation);

    public static readonly Error InvalidLicensePlate = new(
        "vehicle.invalidLicensePlate",
        "Informe uma placa válida no formato antigo (ABC1234) ou Mercosul (ABC1D23).",
        ErrorType.Validation);

    public static readonly Error InvalidBrand = new(
        "vehicle.invalidBrand",
        "Informe a marca do veículo com até 60 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidModel = new(
        "vehicle.invalidModel",
        "Informe o modelo do veículo com até 60 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidYear = new(
        "vehicle.invalidYear",
        "Informe um ano entre 1900 e o ano seguinte ao atual.",
        ErrorType.Validation);

    public static readonly Error HasOpenServiceOrder = new(
        "vehicle.hasOpenServiceOrder",
        "Não é possível excluir o veículo porque ele possui ordem de serviço em aberto.",
        ErrorType.Conflict);

    public static readonly Error DuplicatedLicensePlate = new(
        "vehicle.duplicatedLicensePlate",
        "Já existe um veículo cadastrado com essa placa no sistema.",
        ErrorType.Conflict);
}
