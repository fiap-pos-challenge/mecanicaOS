using NSubstitute;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Mappers;
using Oficina.Application.Users.Queries;
using Oficina.Application.Users.Queries.ListUsers;
using Oficina.Domain.Identity.Enums;

namespace Oficina.UnitTests.Application.Users.Queries;

public sealed class ListUsersQueryHandlerTests
{
    private readonly IUserQueries _queries = Substitute.For<IUserQueries>();
    private readonly UserDto _user = UserTestData.User().ToDto();

    [Fact]
    public async Task Handle_ReturnsThePageFromTheQueries()
    {
        var page = new PagedResult<UserDto>(new[] { _user }, 1, 20, 1);
        _queries.ListAsync(new PageRequest(1, 20), new UserFilter(Role.Mechanic, UserStatus.Active), Arg.Any<CancellationToken>()).Returns(page);

        var result = await new ListUsersQueryHandler(_queries).Handle(new ListUsersQuery(1, 20, Role.Mechanic, UserStatus.Active), CancellationToken.None);

        Assert.Same(page, result.Value);
    }
}
