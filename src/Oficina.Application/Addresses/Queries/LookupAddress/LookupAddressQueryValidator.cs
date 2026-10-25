using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Customers.Errors;

namespace Oficina.Application.Addresses.Queries.LookupAddress;

public sealed class LookupAddressQueryValidator : AbstractValidator<LookupAddressQuery>
{
    public LookupAddressQueryValidator() =>
        RuleFor(query => query.ZipCode)
            .Must(zipCode => zipCode is not null && zipCode.Count(char.IsAsciiDigit) == 8 && zipCode.All(character => char.IsAsciiDigit(character) || character is '-' or '.' or ' '))
            .WithError(CustomerErrors.InvalidZipCode);
}
