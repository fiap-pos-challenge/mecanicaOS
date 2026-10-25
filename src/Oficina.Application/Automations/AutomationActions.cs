namespace Oficina.Application.Automations;

public static class AutomationActions
{
    public const string SendWhatsApp = "SendWhatsApp";
    public const string SendEmail = "SendEmail";
    public const string NotifyTeam = "NotifyTeam";
    public const string CreatePurchaseOrder = "CreatePurchaseOrder";
    public const string RegisterPayment = "RegisterPayment";

    public static readonly IReadOnlyList<string> All = [SendWhatsApp, SendEmail, NotifyTeam, CreatePurchaseOrder, RegisterPayment];

    public static bool NeedsMessage(string kind) => kind is SendWhatsApp or SendEmail or NotifyTeam;
}
