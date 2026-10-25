namespace Oficina.Application.Settings.Dtos;

public sealed record SessionDto(Guid Id, string UserName, string Device, DateTimeOffset LastSeenAt, bool Current);
