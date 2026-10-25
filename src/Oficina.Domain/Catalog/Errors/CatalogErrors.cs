using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Catalog.Errors;

public static class CatalogErrors
{
    public static readonly Error InvalidName = new(
        "catalog.invalidName",
        "Informe o nome do serviço com 2 a 120 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidEstimatedMinutes = new(
        "catalog.invalidEstimatedMinutes",
        "O tempo estimado do serviço deve ser maior que zero.",
        ErrorType.Validation);

    public static readonly Error InvalidBasePrice = new(
        "catalog.invalidBasePrice",
        "O preço base do serviço deve ser maior que zero.",
        ErrorType.Validation);

    public static readonly Error InvalidCommissionPercent = new(
        "catalog.invalidCommissionPercent",
        "Informe a comissão entre 0% e 50%.",
        ErrorType.Validation);

    public static readonly Error InvalidWarrantyDays = new(
        "catalog.invalidWarrantyDays",
        "Informe a garantia entre 0 e 3650 dias.",
        ErrorType.Validation);

    public static readonly Error InvalidUsualParts = new(
        "catalog.invalidUsualParts",
        "Escolha peças cadastradas, sem repetir, com quantidade maior que zero.",
        ErrorType.Validation,
        [new FieldError("usualParts", "catalog.invalidUsualParts", "Escolha peças cadastradas, sem repetir, com quantidade maior que zero.")]);

    public static readonly Error NotFound = new(
        "catalog.notFound",
        "Serviço não encontrado.",
        ErrorType.NotFound);

    public static readonly Error AlreadyInactive = new(
        "catalog.alreadyInactive",
        "O serviço já está inativo.",
        ErrorType.Conflict);

    public static readonly Error AlreadyActive = new(
        "catalog.alreadyActive",
        "O serviço já está ativo.",
        ErrorType.Conflict);

    public static readonly Error CannotChangeInactive = new(
        "catalog.cannotChangeInactive",
        "O serviço está inativo. Reative-o antes de alterar.",
        ErrorType.Conflict);

    public static readonly Error ServiceInactive = new(
        "catalog.serviceInactive",
        "O serviço está inativo e não pode ser incluído na ordem de serviço.",
        ErrorType.BusinessRule);

    public const string UsedByActivePackageCode = "catalog.usedByActivePackage";

    public static Error UsedByActivePackage(IEnumerable<string> packageNames) => new(
        UsedByActivePackageCode,
        $"O item está em pacotes ativos: {string.Join(", ", packageNames)}. Desative os pacotes antes.",
        ErrorType.Conflict);
}
