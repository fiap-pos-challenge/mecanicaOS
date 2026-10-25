namespace Oficina.Application.Finance.Dtos;

public sealed record PlatformPlanDto(string Name, DateTimeOffset RenewsAt, int Users, int MaxUsers);
