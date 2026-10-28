using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.SendQuote;

public sealed class SendQuoteCommandValidator : AbstractValidator<SendQuoteCommand>
{
    public SendQuoteCommandValidator() => RuleFor(command => command.ServiceOrderId).NotEmpty()
        .WithError(CommonErrors.RequiredField);
}
