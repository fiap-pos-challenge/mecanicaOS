namespace Oficina.Domain.Quotes.Entities;

public sealed record DiscountApproval(Guid Id, string Status, string Type, decimal Value, string Reason, string RequestedBy, DateTimeOffset RequestedAt, string? DecidedBy)
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public bool Covers(ManualDiscount discount) => Status == Approved && Type == discount.Type && Value == discount.Value;
}
