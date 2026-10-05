using NSubstitute;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Application.Users.Queries;
using Oficina.Application.Users.Queries.SearchUserByEmail;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Users.Queries;

public sealed class SearchUserByEmailQueryHandlerTests
{
    private readonly IUserQueries _queries = Substitute.For<IUserQueries>();
    private readonly UserDto _user = UserTestData.User().ToDto();

    [Fact]
    public async Task Handle_NormalizesTheEmail()
    {
        _queries.GetByEmailAsync(Email.Create("ana@oficina.com").Value, Arg.Any<CancellationToken>()).Returns(_user);

        var result = await new SearchUserByEmailQueryHandler(_queries).Handle(new SearchUserByEmailQuery(" ANA@oficina.com "), CancellationToken.None);

        Assert.Same(_user, result.Value);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsInvalidEmail()
    {
        var result = await new SearchUserByEmailQueryHandler(_queries).Handle(new SearchUserByEmailQuery("invalid"), CancellationToken.None);

        Assert.Equal(CommonErrors.InvalidEmail, result.Error);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsNotFound()
    {
        var result = await new SearchUserByEmailQueryHandler(_queries).Handle(new SearchUserByEmailQuery("x@oficina.com"), CancellationToken.None);

        Assert.Equal(IdentityErrors.NotFound, result.Error);
    }
}
