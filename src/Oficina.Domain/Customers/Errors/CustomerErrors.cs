using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Customers.Errors;

public static class CustomerErrors
{
    public static readonly Error EmailConsentWithoutEmail = new(
        "customer.emailConsentWithoutEmail",
        "Para aceitar mensagens por e-mail, informe o e-mail do cliente.",
        ErrorType.Validation);

    public static readonly Error InvalidName = new(
        "customer.invalidName",
        "Informe o nome do cliente com 2 a 160 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidDocument = new(
        "customer.invalidDocument",
        "Informe um CPF ou CNPJ válido.",
        ErrorType.Validation);

    public static readonly Error InvalidPhone = new(
        "customer.invalidPhone",
        "Informe um telefone válido com DDD (10 ou 11 dígitos).",
        ErrorType.Validation);

    public static readonly Error IncompleteAddress = new(
        "customer.incompleteAddress",
        "O endereço é opcional, mas, se informado, precisa ter CEP, logradouro, número, bairro, cidade e UF.",
        ErrorType.Validation);

    public static readonly Error InvalidZipCode = new(
        "customer.invalidZipCode",
        "Informe um CEP válido com 8 dígitos.",
        ErrorType.Validation);

    public static readonly Error InvalidState = new(
        "customer.invalidState",
        "Informe uma UF brasileira válida.",
        ErrorType.Validation);

    public static readonly Error InvalidAddress = new(
        "customer.invalidAddress",
        "Um ou mais campos do endereço excedem o tamanho permitido.",
        ErrorType.Validation);

    public static readonly Error HasOpenServiceOrder = new(
        "customer.hasOpenServiceOrder",
        "Não é possível excluir o cliente porque ele possui ordem de serviço em aberto.",
        ErrorType.Conflict);

    public static readonly Error HasActiveVehicles = new(
        "customer.hasActiveVehicles",
        "Não é possível excluir o cliente porque ele possui veículos cadastrados. Exclua os veículos antes.",
        ErrorType.Conflict);

    public static readonly Error NotFound = new(
        "customer.notFound",
        "Cliente não encontrado.",
        ErrorType.NotFound);

    public static readonly Error DuplicateDocument = new(
        "customer.duplicateDocument",
        "Já existe um cliente cadastrado com este CPF ou CNPJ.",
        ErrorType.Conflict);
}
