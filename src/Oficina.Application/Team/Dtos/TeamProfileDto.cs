namespace Oficina.Application.Team.Dtos;

public sealed record TeamProfileDto(
    Guid UserId,
    IReadOnlyList<string> Specialties,
    decimal CommissionPercent,
    decimal DiscountLimitPercent,
    IReadOnlyList<WorkShiftDto> Schedule,
    DateTimeOffset? UpdatedAt);
