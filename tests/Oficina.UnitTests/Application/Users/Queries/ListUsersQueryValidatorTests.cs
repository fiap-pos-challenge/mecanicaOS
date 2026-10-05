using Oficina.Application.Users.Queries.ListUsers;

namespace Oficina.UnitTests.Application.Users.Queries;

public sealed class ListUsersQueryValidatorTests
{
    [Theory]
    [InlineData(1, 20, null)]
    [InlineData(0, 20, "common.invalidPage")]
    [InlineData(1, 101, "common.invalidPageSize")]
    public void Validate_ChecksThePage(int page, int pageSize, string? code)
    {
        var errors = new ListUsersQueryValidator().Validate(new ListUsersQuery(page, pageSize)).Errors;

        Assert.Equal(code, errors.SingleOrDefault()?.ErrorCode);
    }
}
