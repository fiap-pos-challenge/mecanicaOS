using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.ServiceOrders.Commands.RejectQuoteByToken;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Common.Results;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.ServiceOrders.Commands;

public sealed class RejectQuoteByTokenCommandHandlerTests
{
    private const string RawToken = "token-do-link";
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ServiceOrder _order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
    private readonly RejectQuoteByTokenCommandHandler _handler;

    public RejectQuoteByTokenCommandHandlerTests()
    {
        _serviceOrders.GetByApprovalTokenAsync(ApprovalToken.FromRawToken(RawToken).Value, Arg.Any<CancellationToken>()).Returns(_order);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new RejectQuoteByTokenCommandHandler(_serviceOrders, _queries, _unitOfWork, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_ValidToken_RejectsWithoutUserAndReturnsThePublicView()
    {
        var publicView = ServiceOrderTestData.Public();
        _queries.GetPublicByIdAsync(_order.Id, Arg.Any<CancellationToken>()).Returns(publicView);

        var result = await _handler.Handle(new RejectQuoteByTokenCommand(RawToken, "Muito caro"), CancellationToken.None);

        Assert.Same(publicView, result.Value);
        Assert.Equal(ServiceOrderStatus.InDiagnosis, _order.Status);
        Assert.Null(_order.StatusHistory.Last().ChangedByUserId);
        Assert.Null(_order.CurrentQuote!.ApprovalToken);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFound()
    {
        var result = await _handler.Handle(new RejectQuoteByTokenCommand("outro", "Muito caro"), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankToken_ReturnsInvalidApprovalToken()
    {
        var result = await _handler.Handle(new RejectQuoteByTokenCommand(" ", "Muito caro"), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.InvalidApprovalToken, result.Error);
    }

    [Fact]
    public async Task Handle_BlankReason_ReturnsRejectionReasonRequired()
    {
        var result = await _handler.Handle(new RejectQuoteByTokenCommand(RawToken, " "), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.RejectionReasonRequired, result.Error);
        Assert.Equal(ServiceOrderStatus.WaitingApproval, _order.Status);
    }

    [Fact]
    public async Task Handle_PublicViewMissingAfterCommit_ReturnsNotFound()
    {
        var result = await _handler.Handle(new RejectQuoteByTokenCommand(RawToken, "Muito caro"), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.NotFound, result.Error);
    }
}
