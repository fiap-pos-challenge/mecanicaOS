namespace Oficina.Application.Automations.Queries;

public sealed record ScanSubject(string Trigger, Guid SubjectId, string DedupeKey);
