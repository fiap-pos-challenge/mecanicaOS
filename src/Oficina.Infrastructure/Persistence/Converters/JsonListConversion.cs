using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oficina.Infrastructure.Persistence.Converters;

public static class JsonListConversion
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static PropertyBuilder<T?> HasJsonObjectConversion<T>(this PropertyBuilder<T?> property)
        where T : class =>
        property.HasConversion(
            value => value == null ? null : JsonSerializer.Serialize(value, Options),
            json => json == null ? null : JsonSerializer.Deserialize<T>(json, Options),
            new ValueComparer<T?>(
                (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
                value => JsonSerializer.Serialize(value, Options).GetHashCode(StringComparison.Ordinal),
                value => value == null ? null : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)));

    public static PropertyBuilder<T> HasJsonValueConversion<T>(this PropertyBuilder<T> property)
        where T : class =>
        property.HasConversion(
            value => JsonSerializer.Serialize(value, Options),
            json => JsonSerializer.Deserialize<T>(json, Options)!,
            new ValueComparer<T>(
                (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
                value => JsonSerializer.Serialize(value, Options).GetHashCode(StringComparison.Ordinal),
                value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!));

    public static PropertyBuilder<List<T>> HasJsonConversion<T>(this PropertyBuilder<List<T>> property) =>
        property.HasConversion(
            value => JsonSerializer.Serialize(value, Options),
            json => JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>(),
            new ValueComparer<List<T>>(
                (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
                value => JsonSerializer.Serialize(value, Options).GetHashCode(StringComparison.Ordinal),
                value => JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(value, Options), Options) ?? new List<T>()));
}
