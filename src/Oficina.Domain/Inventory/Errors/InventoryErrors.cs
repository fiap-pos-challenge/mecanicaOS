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

    public static readonly Error InvalidSupplier = new(
        "inventory.invalidSupplier",
        "Informe o fornecedor com até 120 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidBarcode = new(
        "inventory.invalidBarcode",
        "Informe um código de barras EAN-13 válido.",
        ErrorType.Validation);

    public static readonly Error DuplicatePartCode = new(
        "inventory.duplicatePartCode",
        "Já existe uma peça ou insumo com este código.",
        ErrorType.Conflict,
        [new FieldError("code", "inventory.duplicatePartCode", "Já existe uma peça ou insumo com este código.")]);

    public static readonly Error NotFound = new(
        "inventory.partNotFound",
        "Peça ou insumo não encontrado.",
        ErrorType.NotFound);

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

    public static readonly Error FractionalPart = new(
        "inventory.fractionalPart",
        "Peça é contada em unidades inteiras; só insumo aceita fração.",
        ErrorType.Validation,
        [new FieldError("quantity", "inventory.fractionalPart", "Peça é contada em unidades inteiras; só insumo aceita fração.")]);

    public static readonly Error UnknownBarcode = new("inventory.unknownBarcode", "Nenhuma peça ou insumo com este código de barras.", ErrorType.NotFound);

    public static readonly Error InvalidSupplierName = new("inventory.invalidSupplierName", "Informe o nome do fornecedor com 2 a 120 caracteres.", ErrorType.Validation,
        [new FieldError("name", "inventory.invalidSupplierName", "Informe o nome do fornecedor com 2 a 120 caracteres.")]);

    public static readonly Error InvalidSupplierDocument = new("inventory.invalidSupplierDocument", "Informe um CNPJ válido.", ErrorType.Validation,
        [new FieldError("document", "inventory.invalidSupplierDocument", "Informe um CNPJ válido.")]);

    public static readonly Error InvalidSupplierEmail = new("common.invalidEmail", "Informe um e-mail válido.", ErrorType.Validation,
        [new FieldError("email", "common.invalidEmail", "Informe um e-mail válido.")]);

    public static readonly Error InvalidLeadTime = new("inventory.invalidLeadTime", "Informe o prazo de entrega entre 0 e 365 dias.", ErrorType.Validation,
        [new FieldError("leadTimeDays", "inventory.invalidLeadTime", "Informe o prazo de entrega entre 0 e 365 dias.")]);

    public static readonly Error DuplicateSupplier = new("inventory.duplicateSupplier", "Já existe um fornecedor com este CNPJ.", ErrorType.Conflict);

    public static readonly Error SupplierNotFound = new("inventory.supplierNotFound", "Fornecedor não encontrado.", ErrorType.NotFound);

    public static readonly Error UnknownLocation = new("inventory.unknownLocation", "Nenhuma peça ativa neste local.", ErrorType.Validation,
        [new FieldError("location", "inventory.unknownLocation", "Nenhuma peça ativa neste local.")]);

    public static readonly Error CountAlreadyOpen = new("inventory.countAlreadyOpen", "Já existe uma contagem aberta para este local.", ErrorType.Conflict);

    public static readonly Error CountNotFound = new("inventory.countNotFound", "Contagem não encontrada.", ErrorType.NotFound);

    public static readonly Error CountClosed = new("inventory.countClosed", "A contagem já foi enviada.", ErrorType.Conflict);

    public static readonly Error CountIncomplete = new("inventory.countIncomplete", "Conte todos os itens antes de enviar.", ErrorType.BusinessRule);

    public static readonly Error CountNotPending = new("inventory.countNotPending", "A contagem não está aguardando aprovação.", ErrorType.Conflict);

    public static readonly Error RejectionReasonRequired = new("common.required", "Informe o motivo.", ErrorType.Validation,
        [new FieldError("reason", "common.required", "Informe o motivo.")]);

    public static readonly Error InvalidOrder = new("inventory.invalidOrder", "Escolha o fornecedor e ao menos uma peça com quantidade e custo.", ErrorType.Validation,
        [new FieldError("lines", "inventory.invalidOrder", "Escolha o fornecedor e ao menos uma peça com quantidade e custo.")]);

    public static readonly Error OrderNotFound = new("inventory.orderNotFound", "Pedido de compra não encontrado.", ErrorType.NotFound);

    public static readonly Error OrderAlreadySent = new("inventory.orderAlreadySent", "O pedido já foi enviado.", ErrorType.Conflict);

    public static readonly Error InvalidInvoiceFile = new("inventory.invalidInvoiceFile", "Envie o XML da nota fiscal eletrônica (NF-e).", ErrorType.Validation,
        [new FieldError("file", "inventory.invalidInvoiceFile", "Envie o XML da nota fiscal eletrônica (NF-e).")]);

    public static readonly Error InvalidInvoiceLines = new("inventory.invalidInvoiceLines", "Associe cada item da nota a uma peça e informe quantidades válidas.", ErrorType.Validation,
        [new FieldError("lines", "inventory.invalidInvoiceLines", "Associe cada item da nota a uma peça e informe quantidades válidas.")]);

    public static readonly Error InvalidUnitCost = new("catalog.invalidPrice", "Informe um custo maior ou igual a zero.", ErrorType.Validation,
        [new FieldError("unitCost", "catalog.invalidPrice", "Informe um custo maior ou igual a zero.")]);
}
