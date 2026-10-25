namespace Oficina.Application.Automations.Dtos;

public sealed record QueueFailureDto(Guid Id, string Queue, string Summary, string Error, int Attempts, DateTimeOffset FailedAt);
