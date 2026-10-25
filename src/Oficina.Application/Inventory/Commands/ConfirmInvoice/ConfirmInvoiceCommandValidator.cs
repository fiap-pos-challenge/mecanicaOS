using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Commands.ConfirmInvoice;

public sealed class ConfirmInvoiceCommandValidator : AbstractValidator<ConfirmInvoiceCommand>
{
    public ConfirmInvoiceCommandValidator()
    {
        RuleFor(request => request.InvoiceNumber).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
