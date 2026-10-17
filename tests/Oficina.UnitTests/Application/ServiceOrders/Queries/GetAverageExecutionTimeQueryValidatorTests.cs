using Oficina.Application.ServiceOrders.Queries.GetAverageExecutionTime;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.UnitTests.Application.ServiceOrders.Queries;

public sealed class GetAverageExecutionTimeQueryValidatorTests
{
    private readonly GetAverageExecutionTimeQueryValidator _validator = new();

    public static TheoryData<DateOnly?, DateOnly?> ValidPeriods => new()
    {
        { null, null },
        { new DateOnly(2026, 10, 1), null },
        { null, new DateOnly(2026, 10, 1) },
        { new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 1) },
        { new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31) },
    };

    [Theory]
    [MemberData(nameof(ValidPeriods))]
    public void Validate_ValidPeriod_ReturnsNoErrors(DateOnly? from, DateOnly? to)
    {
        Assert.True(_validator.Validate(new GetAverageExecutionTimeQuery(from, to)).IsValid);
    }

    [Fact]
    public void Validate_FromAfterTo_ReturnsInvalidPeriod()
    {
        var failure = Assert.Single(_validator.Validate(new GetAverageExecutionTimeQuery(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1))).Errors);
        Assert.Equal(ServiceOrderErrors.InvalidPeriod.Code, failure.ErrorCode);
    }
}
