using Oficina.Application.Common.Pagination;

namespace Oficina.UnitTests.Application.Common.Pagination;

public sealed class PageRequestTests
{
    [Theory]
    [InlineData(1, 20, 0)]
    [InlineData(3, 20, 40)]
    [InlineData(2, 100, 100)]
    public void Skip_IsThePreviousPagesTimesThePageSize(int page, int pageSize, int expected)
    {
        Assert.Equal(expected, new PageRequest(page, pageSize).Skip);
    }
}
