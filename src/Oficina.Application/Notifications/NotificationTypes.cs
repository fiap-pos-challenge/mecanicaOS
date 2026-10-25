namespace Oficina.Application.Notifications;

public static class NotificationTypes
{
    public const string ServiceOrderChanged = "ServiceOrderChanged";
    public const string QuoteAnswered = "QuoteAnswered";
    public const string PaymentConfirmed = "PaymentConfirmed";
    public const string StockLow = "StockLow";
    public const string ExportReady = "ExportReady";
    public const string AutomationFailed = "AutomationFailed";
    public const string DiscountApproval = "DiscountApproval";

    public static readonly IReadOnlyList<string> All = [ServiceOrderChanged, QuoteAnswered, PaymentConfirmed, StockLow, ExportReady, AutomationFailed, DiscountApproval];
}
