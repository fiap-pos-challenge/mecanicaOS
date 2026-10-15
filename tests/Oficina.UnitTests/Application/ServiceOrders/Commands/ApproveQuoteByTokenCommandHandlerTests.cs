using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Commands.ApproveQuoteByToken;
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
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.ServiceOrders.Commands;

public sealed class ApproveQuoteByTokenCommandHandlerTests
{
    private const string RawToken = "token-do-link";
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ServiceOrder _order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
    private readonly ApproveQuoteByTokenCommandHandler _handler;

    public ApproveQuoteByTokenCommandHandlerTests()
    {
        _serviceOrders.GetByApprovalTokenAsync(ApprovalToken.FromRawToken(RawToken).Value, Arg.Any<CancellationToken>()).Returns(_order);
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ServiceOrderBuilder.CreatePart());
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new ApproveQuoteByTokenCommandHandler(_serviceOrders, new QuoteApprovalPolicy(_parts), _queries, _unitOfWork, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_ValidToken_ApprovesWithoutUserCommitsAndReturnsThePublicView()
    {
        var publicView = ServiceOrderTestData.Public();
        _queries.GetPublicByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(publicView);

        var result = await _handler.Handle(new ApproveQuoteByTokenCommand(RawToken), CancellationToken.None);

        Assert.Same(publicView, result.Value);
        Assert.Equal(ServiceOrderStatus.InProgress, _order.Status);
        Assert.Null(_order.StatusHistory.Last().ChangedByUserId);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankToken_ReturnsInvalidApprovalToken()
    {
        var result = await _handler.Handle(new ApproveQuoteByTokenCommand(" "), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.InvalidApprovalToken, result.Error);
        await _serviceOrders.DidNotReceive().GetByApprovalTokenAsync(Arg.Any<ApprovalToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFound()
    {
        var result = await _handler.Handle(new ApproveQuoteByTokenCommand("outro-token"), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsConflictWithoutCommit()
    {
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ServiceOrderBuilder.CreatePart(stock: 1m));

        var result = await _handler.Handle(new ApproveQuoteByTokenCommand(RawToken), CancellationToken.None);

        Assert.Equal(InventoryErrors.InsufficientStock, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CommitFails_ReturnsTheCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(CommonErrors.ConcurrencyConflict));

        var result = await _handler.Handle(new ApproveQuoteByTokenCommand(RawToken), CancellationToken.None);

        Assert.Equal(CommonErrors.ConcurrencyConflict, result.Error);
    }

    [Fact]
    public async Task Handle_PublicViewMissingAfterCommit_ReturnsNotFound()
    {
        _queries.GetPublicByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns((PublicServiceOrderDto?)null);

        var result = await _handler.Handle(new ApproveQuoteByTokenCommand(RawToken), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
    }
}
