using Oficina.Application.Team.Dtos;

namespace Oficina.Api.Contracts.Team;

public sealed record TeamProfilesQueryRequest(IReadOnlyList<Guid> UserIds);

public sealed record UpdateTeamProfileRequest(IReadOnlyList<string> Specialties, decimal CommissionPercent, decimal DiscountLimitPercent, IReadOnlyList<WorkShiftDto> Schedule);

public sealed record ProductivityRequest(IReadOnlyList<Guid> UserIds, int PeriodDays);

public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);
