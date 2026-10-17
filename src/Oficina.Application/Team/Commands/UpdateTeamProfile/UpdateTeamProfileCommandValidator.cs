using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Team.Errors;

namespace Oficina.Application.Team.Commands.UpdateTeamProfile;

public sealed class UpdateTeamProfileCommandValidator : AbstractValidator<UpdateTeamProfileCommand>
{
    public UpdateTeamProfileCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().WithError(CommonErrors.RequiredField);
        RuleFor(command => command.Specialties).NotNull().WithError(TeamErrors.InvalidSpecialties);
        RuleFor(command => command.Schedule).NotNull().WithError(TeamErrors.InvalidShift);
    }
}
