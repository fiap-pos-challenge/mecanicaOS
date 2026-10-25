namespace Oficina.Application.Automations;

public static class AutomationTriggers
{
    public const string QuoteSent = "QuoteSent";
    public const string ServiceOrderStatusChanged = "ServiceOrderStatusChanged";
    public const string StockBelowMinimum = "StockBelowMinimum";
    public const string ServiceOrderDelivered = "ServiceOrderDelivered";
    public const string VehicleMaintenanceDue = "VehicleMaintenanceDue";
    public const string PaymentOverdue = "PaymentOverdue";
    public const string PaymentReceived = "PaymentReceived";

    public static readonly IReadOnlyList<string> All = [QuoteSent, ServiceOrderStatusChanged, StockBelowMinimum, ServiceOrderDelivered, VehicleMaintenanceDue, PaymentOverdue, PaymentReceived];

    public static string StockDedupeKey(Guid partId, DateTimeOffset now) => $"stock:{partId:N}:{now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)}";
}
