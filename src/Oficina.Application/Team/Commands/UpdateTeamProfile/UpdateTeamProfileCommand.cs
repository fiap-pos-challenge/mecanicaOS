using MediatR;
using Oficina.Application.Team.Dtos;
using Oficina.Domain.Common.Results;

namespace Oficina.Application.Team.Commands.UpdateTeamProfile;

public sealed record UpdateTeamProfileCommand(
    Guid UserId,
    IReadOnlyList<string> Specialties,
    decimal CommissionPercent,
    decimal DiscountLimitPercent,
    IReadOnlyList<WorkShiftDto> Schedule) : IRequest<Result<TeamProfileDto>>;
