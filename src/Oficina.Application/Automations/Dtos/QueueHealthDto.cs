namespace Oficina.Application.Automations.Dtos;

public sealed record QueueHealthDto(string Name, int Processed, int Failed, int Pending);
