using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Common.Errors;

public static class CommonErrors
{
    public static readonly Error ValidationFailed = new(
        "common.validationFailed",
        "Um ou mais campos estão inválidos.",
        ErrorType.Validation);

    public static readonly Error RequiredField = new(
        "common.requiredField",
        "Campo obrigatório.",
        ErrorType.Validation);

    public static readonly Error InvalidFormat = new(
        "common.invalidFormat",
        "Valor em formato inválido.",
        ErrorType.Validation);

    public static readonly Error InvalidPage = new(
        "common.invalidPage",
        "A página deve ser maior ou igual a 1.",
        ErrorType.Validation);

    public static readonly Error InvalidPageSize = new(
        "common.invalidPageSize",
        "O tamanho da página deve estar entre 1 e 100.",
        ErrorType.Validation);

    public static readonly Error NegativeAmount = new(
        "common.negativeAmount",
        "O valor não pode ser negativo.",
        ErrorType.Validation);

    public static readonly Error InvalidQuantity = new(
        "common.invalidQuantity",
        "A quantidade deve ser maior que zero e ter no máximo 3 casas decimais.",
        ErrorType.Validation);

    public static readonly Error InvalidEmail = new(
        "common.invalidEmail",
        "Informe um e-mail válido com até 160 caracteres.",
        ErrorType.Validation);

    public static readonly Error ConcurrencyConflict = new(
        "common.concurrencyConflict",
        "O registro foi alterado por outra pessoa. Recarregue os dados e tente novamente.",
        ErrorType.Conflict);

    public static readonly Error DuplicateRecord = new(
        "common.duplicateRecord",
        "Já existe um registro com esses dados.",
        ErrorType.Conflict);

    public static readonly Error InvalidSearchTerm = new(
        "common.invalidSearch",
        "Revise o termo de busca, os filtros e a ordenação.",
        ErrorType.Validation);

    public static readonly Error Unauthorized = new(
        "common.unauthorized",
        "É necessário estar autenticado para acessar este recurso.",
        ErrorType.Unauthorized);

    public static readonly Error Forbidden = new(
        "common.forbidden",
        "Você não tem permissão para executar esta operação.",
        ErrorType.Forbidden);

    public static readonly Error TooManyRequests = new(
        "common.tooManyRequests",
        "Muitas tentativas em pouco tempo. Aguarde um minuto e tente novamente.",
        ErrorType.Conflict);

    public static readonly Error Unexpected = new(
        "common.unexpected",
        "Não foi possível concluir a operação. Informe o código de rastreio ao suporte.",
        ErrorType.Unexpected);
}
