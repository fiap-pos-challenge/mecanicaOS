using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.ServiceOrders.Commands.SendToQuote;

public sealed class SendToQuoteCommandValidator : AbstractValidator<SendToQuoteCommand>
{
    public SendToQuoteCommandValidator() => RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
}
