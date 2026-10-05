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

    public static readonly Error AlreadyInactive = new(
        "catalog.alreadyInactive",
        "O serviço já está inativo.",
        ErrorType.Conflict);

    public static readonly Error AlreadyActive = new(
        "catalog.alreadyActive",
        "O serviço já está ativo.",
        ErrorType.Conflict);

    public static readonly Error ServiceInactive = new(
        "catalog.serviceInactive",
        "O serviço está inativo e não pode ser incluído na ordem de serviço.",
        ErrorType.BusinessRule);
}
