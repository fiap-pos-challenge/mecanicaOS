using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Inventory.Errors;

public static class InventoryErrors
{
    public static readonly Error InvalidPartCode = new(
        "inventory.invalidPartCode",
        "Informe o código da peça com até 30 caracteres (letras, números, ponto e hífen).",
        ErrorType.Validation);

    public static readonly Error InvalidName = new(
        "inventory.invalidName",
        "Informe o nome da peça ou insumo com 2 a 120 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidUnitOfMeasure = new(
        "inventory.invalidUnitOfMeasure",
        "Informe a unidade de medida com até 10 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidMinimumStock = new(
        "inventory.invalidMinimumStock",
        "O estoque mínimo não pode ser negativo e deve ter no máximo 3 casas decimais.",
        ErrorType.Validation);

    public static readonly Error InvalidReason = new(
        "inventory.invalidReason",
        "Informe o motivo da movimentação de estoque.",
        ErrorType.Validation);

    public static readonly Error InsufficientStock = new(
        "inventory.insufficientStock",
        "Não há saldo suficiente em estoque para esta operação.",
        ErrorType.Conflict);

    public static readonly Error PartUnavailable = new(
        "inventory.partUnavailable",
        "A peça ou insumo não tem saldo suficiente para ser incluído na ordem de serviço.",
        ErrorType.BusinessRule);

    public static readonly Error PartInactive = new(
        "inventory.partInactive",
        "A peça ou insumo está inativo e não pode ser incluído na ordem de serviço.",
        ErrorType.BusinessRule);

    public static readonly Error AlreadyInactive = new(
        "inventory.alreadyInactive",
        "A peça ou insumo já está inativo.",
        ErrorType.Conflict);

    public static readonly Error AlreadyActive = new(
        "inventory.alreadyActive",
        "A peça ou insumo já está ativo.",
        ErrorType.Conflict);
}
