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
        "Informe um perfil válido: Admin, Manager, Attendant ou Mechanic.",
        ErrorType.Validation);

    public static readonly Error InvalidCredentials = new(
        "identity.invalidCredentials",
        "E-mail ou senha inválidos.",
        ErrorType.Unauthorized);

    public static readonly Error AccountDisabled = new(
        "identity.accountDisabled",
        "Sua conta está desativada. Fale com o administrador da oficina.",
        ErrorType.Forbidden);

    public static readonly Error InvitationNotFound = new(
        "identity.invitationNotFound",
        "Convite não encontrado. Peça um novo convite ao administrador da oficina.",
        ErrorType.NotFound);

    public static readonly Error InvitationExpired = new(
        "identity.invitationExpired",
        "Este convite expirou. Peça um novo convite ao administrador da oficina.",
        ErrorType.BusinessRule);

    public static readonly Error InvitationAlreadyUsed = new(
        "identity.invitationAlreadyUsed",
        "Este convite já foi usado ou substituído por um novo.",
        ErrorType.Conflict);

    public static readonly Error InvitationNotPending = new(
        "identity.invitationNotPending",
        "Este usuário já definiu a senha; não há convite para reenviar.",
        ErrorType.Conflict);

    public static Error AccountLocked(TimeSpan retryAfter) => new(
        "identity.accountLocked",
        "Conta bloqueada por excesso de tentativas. Aguarde o tempo indicado e tente novamente.",
        ErrorType.TooManyRequests)
    {
        RetryAfter = retryAfter,
    };

    public static readonly Error InvalidTwoFactorCode = new(
        "identity.invalidTwoFactorCode",
        "Código de verificação inválido ou expirado. Entre de novo.",
        ErrorType.Unauthorized);

    public static readonly Error SessionExpired = new(
        "identity.sessionExpired",
        "Sua sessão expirou. Entre de novo.",
        ErrorType.Unauthorized);

    public static readonly Error ReauthenticationFailed = new(
        "auth.reauthenticationFailed",
        "Senha ou código inválido.",
        ErrorType.BusinessRule);

    public static readonly Error ReauthenticationRequired = new(
        "settings.reauthenticationRequired",
        "Confirme sua identidade para continuar.",
        ErrorType.Forbidden);

    public static readonly Error TwoFactorNotSetUp = new(
        "identity.twoFactorNotSetUp",
        "Gere a chave de verificação em duas etapas antes de ativar.",
        ErrorType.Conflict);

    public static readonly Error SessionNotFound = new(
        "identity.sessionNotFound",
        "Sessão não encontrada.",
        ErrorType.NotFound);
}
