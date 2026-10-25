using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.AddPhoto;

public sealed class AddPhotoCommandValidator : AbstractValidator<AddPhotoCommand>
{
    public AddPhotoCommandValidator() => RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
