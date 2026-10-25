using FluentValidation;

namespace Oficina.Application.Quotes.Commands.SaveQuoteDraft;

public sealed class SaveQuoteDraftCommandValidator : AbstractValidator<SaveQuoteDraftCommand>
{
    public SaveQuoteDraftCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
        RuleFor(command => command.Draft.Items).NotNull().WithErrorCode(QuoteValidationCodes.Items).WithMessage(QuoteValidationCodes.ItemsMessage);
    }
}
