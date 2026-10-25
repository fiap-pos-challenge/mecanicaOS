namespace Oficina.Application.Automations.Dtos;

public sealed record QueueFailureListDto(IReadOnlyList<QueueFailureDto> Items);
