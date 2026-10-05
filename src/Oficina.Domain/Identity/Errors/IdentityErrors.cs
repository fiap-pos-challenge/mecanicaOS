using Oficina.Domain.Common.Results;

namespace Oficina.Domain.Identity.Errors;

public static class IdentityErrors
{
    public static readonly Error InvalidName = new(
        "identity.invalidName",
        "Informe o nome do usuário com 2 a 120 caracteres.",
        ErrorType.Validation);

    public static readonly Error InvalidPasswordHash = new(
        "identity.invalidPasswordHash",
        "Não foi possível registrar a senha do usuário.",
        ErrorType.Validation);

    public static readonly Error AlreadyInactive = new(
        "identity.alreadyInactive",
        "O usuário já está inativo.",
        ErrorType.Conflict);

    public static readonly Error AlreadyActive = new(
        "identity.alreadyActive",
        "O usuário já está ativo.",
        ErrorType.Conflict);

    public static readonly Error CannotManageOwnAccount = new(
        "identity.cannotManageOwnAccount",
        "Você não pode excluir, desativar nem rebaixar o seu próprio usuário.",
        ErrorType.BusinessRule);

    public static readonly Error LastActiveAdmin = new(
        "identity.lastActiveAdmin",
        "O sistema precisa ter pelo menos um administrador ativo.",
        ErrorType.Conflict);

    public static readonly Error NotFound = new(
        "identity.notFound",
        "Usuário não encontrado.",
        ErrorType.NotFound);

    public static readonly Error DuplicateEmail = new(
        "identity.duplicateEmail",
        "Já existe um usuário cadastrado com este e-mail.",
        ErrorType.Conflict);

    public static readonly Error WeakPassword = new(
        "identity.weakPassword",
        "A senha deve ter de 8 a 64 caracteres, com letra maiúscula, letra minúscula, número e símbolo.",
        ErrorType.Validation);

    public static readonly Error InvalidRole = new(
        "identity.invalidRole",
        "Informe um perfil válido: Admin, Attendant ou Mechanic.",
        ErrorType.Validation);

    public static readonly Error InvalidCredentials = new(
        "identity.invalidCredentials",
        "E-mail ou senha inválidos.",
        ErrorType.Unauthorized);
}
