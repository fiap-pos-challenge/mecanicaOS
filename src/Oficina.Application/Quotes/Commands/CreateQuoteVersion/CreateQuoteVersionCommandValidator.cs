using FluentValidation;

namespace Oficina.Application.Quotes.Commands.CreateQuoteVersion;

public sealed class CreateQuoteVersionCommandValidator : AbstractValidator<CreateQuoteVersionCommand>
{
    public CreateQuoteVersionCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
    }
}
