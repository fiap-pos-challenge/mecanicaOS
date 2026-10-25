namespace Oficina.Application.Automations;

public static class AutomationMessages
{
    public const string Name = "Dê um nome com 3 a 80 caracteres.";
    public const string NoActions = "Adicione pelo menos uma ação.";
    public const string TooManyConditions = "Use até 5 condições.";
    public const string TooManyActions = "Use até 5 ações.";
    public const string ConditionTrigger = "Esta condição não vale para o gatilho escolhido.";
    public const string Operator = "Operador inválido para a condição.";
    public const string Value = "Valor inválido na condição.";
    public const string Wait = "Tempo de espera inválido.";
    public const string ActionTrigger = "Esta ação não vale para o gatilho escolhido.";
    public const string Message = "Escreva a mensagem da ação.";
    public const string MessageLength = "A mensagem pode ter até 500 caracteres.";
    public const string Placeholders = "Use só {cliente}, {os}, {veiculo}, {valor} e {link}.";
    public const string RuleOff = "Regra desligada ou removida";
    public const string EmailSent = "e-mail enviado";
    public const string EmailNoConsent = "e-mail não enviado: sem consentimento";
    public const string WhatsAppNoConsent = "WhatsApp não enviado: sem consentimento";
    public const string WhatsAppQueued = "WhatsApp na fila";
    public const string TeamNotified = "equipe avisada";
    public const string PaymentRegistered = "pagamento registrado";
    public const string DefaultSubject = "Sua oficina";

    public static string NotFound(string name) => $"{name}: registro não encontrado";

    public static string ConditionsNotMet(string name) => $"{name}: condições não atendidas";

    public static string Done(string name, IEnumerable<string> steps) => $"{name}: {string.Join("; ", steps)}";

    public static string FailureTitle(string name) => $"Falha na automação {name}";

    public static string FailureBody(string error) => $"{error} O item está na fila de erros.";

    public static string PurchaseOrderCreated(long number) => $"pedido de compra nº {number} criado";

    public static string Order(long number) => $"OS nº {number}";
}
