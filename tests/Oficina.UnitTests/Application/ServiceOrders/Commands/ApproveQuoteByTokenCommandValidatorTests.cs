using Oficina.Application.ServiceOrders.Commands.ApproveQuoteByToken;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.UnitTests.Application.ServiceOrders.Commands;

public sealed class ApproveQuoteByTokenCommandValidatorTests
{
    private readonly ApproveQuoteByTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WithToken_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(new ApproveQuoteByTokenCommand("token-do-link")).IsValid);
    }

    [Fact]
    public void Validate_EmptyToken_ReturnsInvalidApprovalToken()
    {
        var failure = Assert.Single(_validator.Validate(new ApproveQuoteByTokenCommand("")).Errors);
        Assert.Equal(ServiceOrderErrors.InvalidApprovalToken.Code, failure.ErrorCode);
    }
}
