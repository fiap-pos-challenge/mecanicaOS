namespace Oficina.Application.Team.Dtos;

public sealed record WorkShiftDto(string Day, string Start, string End);

public sealed record TeamProfileDto(
    Guid UserId,
    IReadOnlyList<string> Specialties,
    decimal CommissionPercent,
    decimal DiscountLimitPercent,
    IReadOnlyList<WorkShiftDto> Schedule,
    DateTimeOffset? UpdatedAt);

public sealed record TeamProfileListDto(IReadOnlyList<TeamProfileDto> Items);

public sealed record ProductivityDto(
    Guid UserId,
    int ServiceOrders,
    decimal Hours,
    decimal OccupancyPercent,
    decimal Revenue,
    decimal ReworkPercent,
    IReadOnlyList<int> Weekly);

public sealed record ProductivityListDto(int PeriodDays, IReadOnlyList<ProductivityDto> Items);

public sealed record RoleDefinitionDto(string Role, IReadOnlyList<string> Permissions, bool Editable);

public sealed record RoleListDto(bool Customizable, IReadOnlyList<RoleDefinitionDto> Items);
