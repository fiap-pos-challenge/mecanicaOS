using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Catalog.Errors;

public static class PackageErrors
{
    public static readonly Error InvalidName = new(
        "catalog.invalidPackageName",
        "Informe o nome do pacote com 2 a 120 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidDescription = new(
        "catalog.invalidPackageDescription",
        "A descrição do pacote deve ter no máximo 500 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidPrice = new(
        "catalog.invalidPackagePrice",
        "O preço do pacote deve ser maior que zero.",
        ErrorType.Validation);

    public static readonly Error InvalidItems = new(
        "catalog.invalidItems",
        "Inclua de 1 a 50 serviços ou peças ativos, sem repetir, com quantidade maior que zero.",
        ErrorType.Validation,
        [new FieldError("items", "catalog.invalidItems", "Inclua de 1 a 50 serviços ou peças ativos, sem repetir, com quantidade maior que zero.")]);

    public static readonly Error InvalidMileageRange = new(
        "catalog.invalidMileageRange",
        "Informe a faixa de quilometragem com valores positivos e o mínimo menor ou igual ao máximo.",
        ErrorType.Validation);

    public static readonly Error NotFound = new(
        "catalog.packageNotFound",
        "Pacote não encontrado.",
        ErrorType.NotFound);

    public static readonly Error AlreadyInactive = new(
        "catalog.packageAlreadyInactive",
        "O pacote já está inativo.",
        ErrorType.Conflict);

    public static readonly Error AlreadyActive = new(
        "catalog.packageAlreadyActive",
        "O pacote já está ativo.",
        ErrorType.Conflict);

    public static readonly Error CannotChangeInactive = new(
        "catalog.packageCannotChangeInactive",
        "O pacote está inativo. Reative-o antes de alterar.",
        ErrorType.Conflict);
}
