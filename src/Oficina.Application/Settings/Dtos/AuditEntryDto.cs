namespace Oficina.Application.Settings.Dtos;

public sealed record AuditEntryDto(Guid Id, DateTimeOffset OccurredAt, string ActorName, string Area, string Action, string Target, string Detail);
