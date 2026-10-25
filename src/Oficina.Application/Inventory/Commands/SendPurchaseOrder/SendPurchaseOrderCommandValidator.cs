using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Commands.SendPurchaseOrder;

public sealed class SendPurchaseOrderCommandValidator : AbstractValidator<SendPurchaseOrderCommand>
{
    public SendPurchaseOrderCommandValidator()
    {
        RuleFor(request => request.Id).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
