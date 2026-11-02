using NSubstitute;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Application.Users.Queries;
using Oficina.Application.Users.Queries.GetUserById;
using Oficina.Domain.Identity.Errors;

namespace Oficina.UnitTests.Application.Users.Queries;

public sealed class GetUserByIdQueryHandlerTests
{
    private readonly IUserQueries _queries = Substitute.For<IUserQueries>();
    private readonly UserDto _user = UserTestData.User().ToDto();

    [Fact]
    public async Task Handle_ExistingUser_ReturnsItWithoutPasswordHash()
    {
        _queries.GetByIdAsync(_user.Id, Arg.Any<CancellationToken>()).Returns(_user);

        var result = await new GetUserByIdQueryHandler(_queries).Handle(new GetUserByIdQuery(_user.Id), CancellationToken.None);

        Assert.Same(_user, result.Value);
        Assert.DoesNotContain(typeof(UserDto).GetProperties(), property => property.Name.Contains("Password", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var result = await new GetUserByIdQueryHandler(_queries).Handle(new GetUserByIdQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(IdentityErrors.NotFound, result.Error);
    }
}
