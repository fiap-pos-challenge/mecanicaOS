namespace Oficina.Application.Jobs.Dtos;

public sealed record ExportAccess(DateTimeOffset OccurredAt, string UserName, string Field, string Reason);
