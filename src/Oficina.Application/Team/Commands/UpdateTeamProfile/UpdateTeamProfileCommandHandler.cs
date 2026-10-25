using MediatR;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Team.Dtos;
using Oficina.Application.Team.Mappers;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Identity.Errors;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Team.Entities;
using Oficina.Domain.Team.Errors;
using Oficina.Domain.Team.Repositories;

namespace Oficina.Application.Team.Commands.UpdateTeamProfile;

public sealed class UpdateTeamProfileCommandHandler(
    IUserRepository users,
    ITeamProfileRepository profiles,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<UpdateTeamProfileCommand, Result<TeamProfileDto>>
{
    public async Task<Result<TeamProfileDto>> Handle(UpdateTeamProfileCommand command, CancellationToken cancellationToken)
    {
        if (await users.GetByIdAsync(command.UserId, cancellationToken) is not { } user)
        {
            return IdentityErrors.NotFound;
        }

        var shifts = command.Schedule.Select(TeamProfileMapper.ToShift).ToList();
        if (shifts.Any(shift => shift is null))
        {
            return TeamErrors.InvalidShift;
        }

        var profile = await profiles.GetAsync(user.Id, cancellationToken);
        var isNew = profile is null;
        profile ??= TeamProfile.Empty(user.Id);
        var changed = profile.Change(user.Role, command.Specialties, command.CommissionPercent, command.DiscountLimitPercent, shifts.OfType<WorkShift>().ToList(), timeProvider.GetUtcNow());
        if (changed.Error is { } invalid)
        {
            return invalid;
        }

        if (isNew)
        {
            profiles.Add(profile);
        }

        if ((await unitOfWork.CommitAsync(cancellationToken)).Error is { } commitError)
        {
            return commitError;
        }

        return profile.ToDto();
    }
}
