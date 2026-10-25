namespace Oficina.Application.Finance.Dtos;

public sealed record PlatformDto(string ShopName, PlatformPlanDto Plan, PlatformUsageDto Usage, IReadOnlyList<PlatformModuleDto> Modules);
