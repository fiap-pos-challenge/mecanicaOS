namespace Oficina.Application.Jobs.Dtos;

public sealed record JobDto(Guid Id, string Kind, string Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, JobResultDto? Result);
