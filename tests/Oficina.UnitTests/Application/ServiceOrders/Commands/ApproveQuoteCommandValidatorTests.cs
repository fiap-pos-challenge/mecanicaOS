using Oficina.Application.ServiceOrders.Commands.ApproveQuote;
using Oficina.Domain.Common.Errors;

namespace Oficina.UnitTests.Application.ServiceOrders.Commands;

public sealed class ApproveQuoteCommandValidatorTests
{
    private readonly ApproveQuoteCommandValidator _validator = new();

    [Fact]
    public void Validate_WithId_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(new ApproveQuoteCommand(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        var failure = Assert.Single(_validator.Validate(new ApproveQuoteCommand(Guid.Empty)).Errors);
        Assert.Equal(CommonErrors.RequiredField.Code, failure.ErrorCode);
    }
}
