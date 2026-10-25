using NSubstitute;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.Identity.Dtos;
using Oficina.Application.Identity.Queries.GetCurrentUser;
using Oficina.Application.Users.Mappers;
using Oficina.Application.Users.Queries;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Identity.Enums;

namespace Oficina.UnitTests.Application.Identity;

public sealed class GetCurrentUserQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserQueries _queries = Substitute.For<IUserQueries>();
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests() => _handler = new GetCurrentUserQueryHandler(_currentUser, _queries);

    [Fact]
    public async Task Handle_LoggedActiveUser_ReturnsIdNameEmailAndRole()
    {
        var user = UserTestData.User(Role.Mechanic);
        _currentUser.UserId.Returns(user.Id);
        _queries.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user.ToDto());

        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.Equal(new CurrentUserDto(user.Id, "Ana Lima", "ana@oficina.com", Role.Mechanic), result.Value);
    }

    [Fact]
    public async Task Handle_Anonymous_ReturnsUnauthorized()
    {
        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.Equal(CommonErrors.Unauthorized, result.Error);
    }

    [Fact]
    public async Task Handle_UserRemovedAfterLogin_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns(Guid.CreateVersion7());

        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.Equal(CommonErrors.Unauthorized, result.Error);
    }

    [Fact]
    public async Task Handle_UserDeactivatedAfterLogin_ReturnsUnauthorized()
    {
        var user = UserTestData.Inactive();
        _currentUser.UserId.Returns(user.Id);
        _queries.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user.ToDto());

        var result = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.Equal(CommonErrors.Unauthorized, result.Error);
    }
}
