using FluentValidation;
using Oficina.Application.Quotes.Delivery;
using Oficina.Application.Settings.Models;
using Oficina.Domain.Quotes.Errors;

namespace Oficina.Application.Quotes.Commands.SendQuote;

public sealed class SendQuoteCommandValidator : AbstractValidator<SendQuoteCommand>
{
    public SendQuoteCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty().WithErrorCode(QuoteValidationCodes.Id).WithMessage(QuoteValidationCodes.IdMessage);
        RuleFor(command => command.Channel).Must(QuoteChannel.All.Contains).WithErrorCode(QuoteErrors.InvalidChannel.Code).WithMessage(QuoteErrors.InvalidChannel.Message);
        RuleFor(command => command.ExpiresInDays).Must(SettingsDefaults.ExpirationDays.Contains).WithErrorCode(QuoteErrors.InvalidExpiration.Code).WithMessage(QuoteErrors.InvalidExpiration.Message);
        RuleFor(command => command.Message).NotEmpty().MaximumLength(QuoteDispatcher.MaxMessageLength).WithErrorCode(QuoteErrors.InvalidMessage.Code).WithMessage(QuoteErrors.InvalidMessage.Message);
    }
}
