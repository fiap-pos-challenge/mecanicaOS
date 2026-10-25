using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Inventory.Errors;

namespace Oficina.Domain.Inventory.Entities;

public sealed class Supplier : AggregateRoot
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 120;
    public const int PhoneMaxLength = 20;
    public const int MaxLeadTimeDays = 365;

    private Supplier(Guid id, string name, string? document, string? email, string? phone, int leadTimeDays, bool isActive)
        : base(id)
    {
        Name = name;
        Document = document;
        Email = email;
        Phone = phone;
        LeadTimeDays = leadTimeDays;
        IsActive = isActive;
    }

    public string Name { get; private set; }

    public string? Document { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public int LeadTimeDays { get; private set; }

    public bool IsActive { get; private set; }

    public static Result<Supplier> Create(string name, string? document, string? email, string? phone, int leadTimeDays)
    {
        var supplier = new Supplier(Guid.CreateVersion7(), string.Empty, null, null, null, 0, isActive: true);
        var changed = supplier.Change(name, document, email, phone, leadTimeDays);
        return changed.Error is { } error ? error : supplier;
    }

    public Result Change(string name, string? document, string? email, string? phone, int leadTimeDays)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length is < NameMinLength or > NameMaxLength)
        {
            return InventoryErrors.InvalidSupplierName;
        }

        string? cnpj = null;
        if (Digits(document) is { Length: > 0 } documentDigits)
        {
            var parsed = Customers.ValueObjects.Document.Create(documentDigits);
            if (parsed.IsFailure || parsed.Value.Type != DocumentType.Cnpj)
            {
                return InventoryErrors.InvalidSupplierDocument;
            }

            cnpj = parsed.Value.Value;
        }

        string? address = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var parsed = Common.ValueObjects.Email.Create(email);
            if (parsed.IsFailure)
            {
                return InventoryErrors.InvalidSupplierEmail;
            }

            address = parsed.Value.Value;
        }

        if (leadTimeDays is < 0 or > MaxLeadTimeDays)
        {
            return InventoryErrors.InvalidLeadTime;
        }

        Name = trimmed;
        Document = cnpj;
        Email = address;
        Phone = Digits(phone) is { Length: > 0 } phoneDigits ? phoneDigits[..Math.Min(phoneDigits.Length, PhoneMaxLength)] : null;
        LeadTimeDays = leadTimeDays;
        return Result.Success();
    }

    private static string? Digits(string? value) => value is null ? null : new string(value.Where(char.IsAsciiDigit).ToArray());
}
