using Oficina.Domain.Common.Events;

namespace Oficina.Infrastructure.Messaging.Outbox;

public static class OutboxEventTypes
{
    private static readonly IReadOnlyDictionary<string, Type> TypesByName = typeof(IDomainEvent).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(type))
        .ToDictionary(type => type.Name, StringComparer.Ordinal);

    public static Type? Find(string name) => TypesByName.GetValueOrDefault(name);
}
