namespace Oficina.Domain.Settings.Entities;

public sealed class SettingsDocument
{
    public const int KeyMaxLength = 40;

    private SettingsDocument(string key, string json, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        Key = key;
        Json = json;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public string Key { get; private init; }

    public string Json { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public static SettingsDocument Create(string key, string json, Guid? updatedBy, DateTimeOffset now) => new(key, json, now, updatedBy);

    public void Replace(string json, Guid? updatedBy, DateTimeOffset now)
    {
        Json = json;
        UpdatedBy = updatedBy;
        UpdatedAt = now;
    }
}
