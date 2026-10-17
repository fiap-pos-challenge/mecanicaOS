using Oficina.Domain.Common.Results;

namespace Oficina.Domain.ServiceOrders.Errors;

public static class ServiceOrderErrors
{
    public static readonly Error InvalidNumber = new(
        "serviceOrder.invalidNumber",
        "O número da ordem de serviço é inválido.",
        ErrorType.Validation);

    public static readonly Error CustomerRequired = new(
        "serviceOrder.customerRequired",
        "Informe o cliente da ordem de serviço.",
        ErrorType.Validation);

    public static readonly Error VehicleRequired = new(
        "serviceOrder.vehicleRequired",
        "Informe o veículo da ordem de serviço.",
        ErrorType.Validation);

    public static readonly Error InvalidProblemDescription = new(
        "serviceOrder.invalidProblemDescription",
        "Descreva o problema relatado com pelo menos 3 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidApprovalToken = new(
        "serviceOrder.invalidApprovalToken",
        "O link de aprovação do orçamento é inválido.",
        ErrorType.Validation);

    public static readonly Error InvalidTransition = new(
        "serviceOrder.invalidTransition",
        "Esta ação não é permitida no status atual da ordem de serviço.",
        ErrorType.Conflict);

    public static readonly Error ItemsOnlyInDiagnosis = new(
        "serviceOrder.itemsOnlyInDiagnosis",
        "Serviços e peças só podem ser alterados enquanto a ordem de serviço está em diagnóstico.",
        ErrorType.Conflict);

    public static readonly Error InvalidServiceQuantity = new(
        "serviceOrder.invalidServiceQuantity",
        "A quantidade do serviço deve ser maior que zero.",
        ErrorType.Validation);

    public static readonly Error ItemNotFound = new(
        "serviceOrder.itemNotFound",
        "O item não foi encontrado na ordem de serviço.",
        ErrorType.NotFound);

    public static readonly Error QuoteWithoutItems = new(
        "serviceOrder.quoteWithoutItems",
        "Inclua pelo menos um serviço ou peça antes de enviar o orçamento.",
        ErrorType.Conflict);

    public static readonly Error RejectionReasonRequired = new(
        "serviceOrder.rejectionReasonRequired",
        "Informe o motivo da reprovação do orçamento.",
        ErrorType.Validation);

    public static readonly Error VehicleHasOpenOrder = new(
        "serviceOrder.vehicleHasOpenOrder",
        "Este veículo já possui uma ordem de serviço em aberto.",
        ErrorType.Conflict);

    public static readonly Error VehicleNotOwnedByCustomer = new(
        "serviceOrder.vehicleNotOwnedByCustomer",
        "O veículo informado não pertence a este cliente.",
        ErrorType.Conflict);

    public static readonly Error NotFound = new(
        "serviceOrder.notFound",
        "Ordem de serviço não encontrada.",
        ErrorType.NotFound);

    public static readonly Error InvalidStatus = new(
        "serviceOrder.invalidStatus",
        "Informe um status válido: Received, InDiagnosis, WaitingApproval, InProgress, Completed ou Delivered.",
        ErrorType.Validation);

    public static readonly Error InvalidPeriod = new(
        "serviceOrder.invalidPeriod",
        "A data inicial deve ser menor ou igual à data final.",
        ErrorType.Validation);

    public static readonly Error VersionConflict = new(
        "serviceOrders.versionConflict",
        "A OS mudou enquanto você editava. Os dados foram atualizados.",
        ErrorType.Conflict);

    public static readonly Error InvalidChecklist = new(
        "serviceOrders.invalidChecklist",
        "Marque cada item do checklist como Ok, Atenção ou Trocar.",
        ErrorType.Validation);

    public static readonly Error InvalidPhotoType = new(
        "serviceOrders.photoType",
        "Envie a foto em JPG, PNG ou WebP.",
        ErrorType.Validation,
        [new FieldError("contentType", "serviceOrders.photoType", "Envie a foto em JPG, PNG ou WebP.")]);

    public static readonly Error PhotoTooLarge = new(
        "serviceOrders.photoSize",
        "A foto passou do tamanho máximo de 5 MB.",
        ErrorType.Validation,
        [new FieldError("dataUrl", "serviceOrders.photoSize", "A foto passou do tamanho máximo de 5 MB.")]);

    public static readonly Error InvalidDescription = new(
        "serviceOrders.invalidDescription",
        "Descreva o problema com pelo menos 3 caracteres.",
        ErrorType.Validation,
        [new FieldError("description", "serviceOrders.invalidDescription", "Descreva o problema com pelo menos 3 caracteres.")]);

    public static readonly Error InvalidPromise = new(
        "serviceOrders.invalidPromise",
        "A data prometida precisa ser futura.",
        ErrorType.Validation);
}
