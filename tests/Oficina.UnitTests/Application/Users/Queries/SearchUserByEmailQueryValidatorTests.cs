using Oficina.Application.Users.Queries.SearchUserByEmail;

namespace Oficina.UnitTests.Application.Users.Queries;

public sealed class SearchUserByEmailQueryValidatorTests
{
    [Theory]
    [InlineData("ana@oficina.com", null)]
    [InlineData("", "common.invalidEmail")]
    [InlineData("ana", "common.invalidEmail")]
    public void Validate_ChecksTheEmail(string email, string? code)
    {
        var errors = new SearchUserByEmailQueryValidator().Validate(new SearchUserByEmailQuery(email)).Errors;

        Assert.Equal(code, errors.SingleOrDefault()?.ErrorCode);
    }
}
