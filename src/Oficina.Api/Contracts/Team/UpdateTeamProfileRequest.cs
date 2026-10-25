using Oficina.Application.Team.Dtos;

namespace Oficina.Api.Contracts.Team;

public sealed record UpdateTeamProfileRequest(IReadOnlyList<string> Specialties, decimal CommissionPercent, decimal DiscountLimitPercent, IReadOnlyList<WorkShiftDto> Schedule);
