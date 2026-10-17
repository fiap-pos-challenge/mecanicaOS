using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;

namespace Oficina.Application.Inventory.Queries.LookupBarcode;

public sealed class LookupBarcodeQueryValidator : AbstractValidator<LookupBarcodeQuery>
{
    public LookupBarcodeQueryValidator()
    {
        RuleFor(request => request.Barcode).NotEmpty().WithError(CommonErrors.RequiredField);
    }
}
