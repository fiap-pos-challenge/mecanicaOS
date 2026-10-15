using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Commands.ApproveQuote;
using Oficina.Application.ServiceOrders.Dtos;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.Services;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.ServiceOrders.Commands;

public sealed class ApproveQuoteCommandHandlerTests
{
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ServiceOrder _order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly ApproveQuoteCommandHandler _handler;

    public ApproveQuoteCommandHandlerTests()
    {
        _serviceOrders.GetByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(_order);
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ServiceOrderBuilder.CreatePart());
        _currentUser.UserId.Returns(_userId);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new ApproveQuoteCommandHandler(_serviceOrders, new QuoteApprovalPolicy(_parts), _queries, _currentUser, _unitOfWork, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_PendingQuote_ApprovesCommitsAndReturnsTheDetail()
    {
        var detail = ServiceOrderTestData.Detail(_order.Id);
        _queries.GetByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(detail);

        var result = await _handler.Handle(new ApproveQuoteCommand(_order.Id), CancellationToken.None);

        Assert.Same(detail, result.Value);
        Assert.Equal(ServiceOrderStatus.InProgress, _order.Status);
        Assert.Equal(_userId, _order.StatusHistory.Last().ChangedByUserId);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownServiceOrder_ReturnsNotFound()
    {
        var result = await _handler.Handle(new ApproveQuoteCommand(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsConflictWithoutCommit()
    {
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ServiceOrderBuilder.CreatePart(stock: 1m));

        var result = await _handler.Handle(new ApproveQuoteCommand(_order.Id), CancellationToken.None);

        Assert.Equal(InventoryErrors.InsufficientStock, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _handler.Handle(new ApproveQuoteCommand(_order.Id), CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
        await _queries.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DetailMissingAfterCommit_ReturnsNotFound()
    {
        _queries.GetByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns((ServiceOrderDetailDto?)null);

        var result = await _handler.Handle(new ApproveQuoteCommand(_order.Id), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
    }
}
