using Oficina.Application.ServiceOrders.Queries.GetServiceOrderById;
using Oficina.Domain.Common.Errors;

namespace Oficina.UnitTests.Application.ServiceOrders.Queries;

public sealed class GetServiceOrderByIdQueryValidatorTests
{
    private readonly GetServiceOrderByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WithId_ReturnsNoErrors()
    {
        Assert.True(_validator.Validate(new GetServiceOrderByIdQuery(Guid.CreateVersion7())).IsValid);
    }

    [Fact]
    public void Validate_EmptyId_ReturnsRequiredField()
    {
        var failure = Assert.Single(_validator.Validate(new GetServiceOrderByIdQuery(Guid.Empty)).Errors);
        Assert.Equal(CommonErrors.RequiredField.Code, failure.ErrorCode);
    }
}
