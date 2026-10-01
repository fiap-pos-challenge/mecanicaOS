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
}
