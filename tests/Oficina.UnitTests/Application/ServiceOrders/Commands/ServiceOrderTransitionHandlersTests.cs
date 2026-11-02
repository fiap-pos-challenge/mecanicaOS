using NSubstitute;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Application.ServiceOrders.Commands;
using Oficina.Application.ServiceOrders.Commands.AddPartItem;
using Oficina.Application.ServiceOrders.Commands.AddServiceItem;
using Oficina.Application.ServiceOrders.Commands.CompleteServiceOrder;
using Oficina.Application.ServiceOrders.Commands.DeliverServiceOrder;
using Oficina.Application.ServiceOrders.Commands.RejectQuote;
using Oficina.Application.ServiceOrders.Commands.RemoveItem;
using Oficina.Application.ServiceOrders.Commands.SendQuote;
using Oficina.Application.ServiceOrders.Commands.StartDiagnosis;
using Oficina.Application.ServiceOrders.Queries;
using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Repositories;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Application.ServiceOrders.Commands;

public sealed class ServiceOrderTransitionHandlersTests
{
    private readonly IServiceOrderRepository _serviceOrders = Substitute.For<IServiceOrderRepository>();
    private readonly IServiceOrderQueries _queries = Substitute.For<IServiceOrderQueries>();
    private readonly IServiceRepository _services = Substitute.For<IServiceRepository>();
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly ServiceOrderChanges _changes;

    public ServiceOrderTransitionHandlersTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _queries.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(call => ServiceOrderTestData.Detail(call.Arg<Guid>()));
        _currentUser.UserId.Returns(_userId);
        _changes = new ServiceOrderChanges(_serviceOrders, _queries, _unitOfWork);
    }

    [Fact]
    public async Task StartDiagnosis_ReceivedOrder_MovesToInDiagnosisByTheCurrentUser()
    {
        var order = Stored(ServiceOrderStatus.Received);

        var result = await new StartDiagnosisCommandHandler(_changes, _currentUser, TimeProvider.System)
            .Handle(new StartDiagnosisCommand(order.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.InDiagnosis, order.Status);
        Assert.Equal(_userId, order.StatusHistory.Last().ChangedByUserId);
    }

    [Fact]
    public async Task Complete_InProgressOrder_MovesToCompleted()
    {
        var order = Stored(ServiceOrderStatus.InProgress);

        var result = await new CompleteServiceOrderCommandHandler(_changes, _currentUser, TimeProvider.System)
            .Handle(new CompleteServiceOrderCommand(order.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedAt);
    }

    [Fact]
    public async Task Deliver_CompletedOrder_MovesToDelivered()
    {
        var order = Stored(ServiceOrderStatus.Completed);

        var result = await new DeliverServiceOrderCommandHandler(_changes, _currentUser, TimeProvider.System)
            .Handle(new DeliverServiceOrderCommand(order.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.Delivered, order.Status);
    }

    [Fact]
    public async Task Deliver_OrderNotCompleted_ReturnsInvalidTransition()
    {
        var order = Stored(ServiceOrderStatus.InProgress);

        var result = await new DeliverServiceOrderCommandHandler(_changes, _currentUser, TimeProvider.System)
            .Handle(new DeliverServiceOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.InvalidTransition, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectQuote_PendingQuote_ReturnsToDiagnosisWithTheReason()
    {
        var order = Stored(ServiceOrderStatus.WaitingApproval);

        var result = await new RejectQuoteCommandHandler(_changes, _currentUser, TimeProvider.System)
            .Handle(new RejectQuoteCommand(order.Id, "Muito caro"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.InDiagnosis, order.Status);
        Assert.Equal(QuoteStatus.Rejected, order.CurrentQuote!.Status);
        Assert.Equal("Muito caro", order.StatusHistory.Last().Note);
    }

    [Fact]
    public async Task RemoveItem_ExistingItem_RemovesIt()
    {
        var order = Stored(ServiceOrderStatus.InDiagnosis);
        order.AddServiceItem(ServiceOrderBuilder.CreateService(), 1);

        var result = await new RemoveItemCommandHandler(_changes)
            .Handle(new RemoveItemCommand(order.Id, order.ServiceItems.Single().Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(order.ServiceItems);
    }

    [Fact]
    public async Task AddServiceItem_ActiveService_AddsItWithTheCatalogPrice()
    {
        var order = Stored(ServiceOrderStatus.InDiagnosis);
        var service = ServiceOrderBuilder.CreateService(price: 150m);
        _services.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        var result = await new AddServiceItemCommandHandler(_services, _changes)
            .Handle(new AddServiceItemCommand(order.Id, service.Id, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(300m, order.ServiceItems.Single().Total.Amount);
    }

    [Fact]
    public async Task AddServiceItem_UnknownService_ReturnsCatalogNotFound()
    {
        var order = Stored(ServiceOrderStatus.InDiagnosis);

        var result = await new AddServiceItemCommandHandler(_services, _changes)
            .Handle(new AddServiceItemCommand(order.Id, Guid.CreateVersion7(), 1), CancellationToken.None);

        Assert.Equal(CatalogErrors.NotFound, result.Error);
        Assert.Empty(order.ServiceItems);
    }

    [Fact]
    public async Task AddPartItem_PartWithStock_AddsIt()
    {
        var order = Stored(ServiceOrderStatus.InDiagnosis);
        var part = ServiceOrderBuilder.CreatePart(price: 25m, stock: 10m);
        _parts.GetByIdAsync(part.Id, Arg.Any<CancellationToken>()).Returns(part);

        var result = await new AddPartItemCommandHandler(_parts, _changes)
            .Handle(new AddPartItemCommand(order.Id, part.Id, 2m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, order.PartItems.Single().Total.Amount);
        Assert.Equal(10m, part.StockQuantity);
    }

    [Fact]
    public async Task AddPartItem_UnknownPartOrInvalidQuantity_ReturnsTheError()
    {
        var order = Stored(ServiceOrderStatus.InDiagnosis);
        var handler = new AddPartItemCommandHandler(_parts, _changes);

        var unknown = await handler.Handle(new AddPartItemCommand(order.Id, Guid.CreateVersion7(), 1m), CancellationToken.None);
        var invalid = await handler.Handle(new AddPartItemCommand(order.Id, Guid.CreateVersion7(), 0m), CancellationToken.None);

        Assert.Equal(InventoryErrors.NotFound, unknown.Error);
        Assert.Equal(CommonErrors.InvalidQuantity, invalid.Error);
    }

    [Fact]
    public async Task SendQuote_OrderWithItems_ReturnsARawTokenThatMatchesTheStoredHash()
    {
        var order = Stored(ServiceOrderStatus.InDiagnosis);
        order.AddServiceItem(ServiceOrderBuilder.CreateService(price: 100m), 1);

        var result = await new SendQuoteCommandHandler(_changes, _currentUser, TimeProvider.System)
            .Handle(new SendQuoteCommand(order.Id), CancellationToken.None);

        Assert.Equal(64, result.Value.ApprovalToken.Length);
        Assert.Equal(ServiceOrderStatus.WaitingApproval, order.Status);
        Assert.True(order.MatchesApprovalToken(ApprovalToken.FromRawToken(result.Value.ApprovalToken).Value));
        Assert.Equal(order.Id, result.Value.ServiceOrder.Id);
    }

    [Fact]
    public async Task SendQuote_OrderWithoutItems_ReturnsQuoteWithoutItems()
    {
        var order = Stored(ServiceOrderStatus.InDiagnosis);

        var result = await new SendQuoteCommandHandler(_changes, _currentUser, TimeProvider.System)
            .Handle(new SendQuoteCommand(order.Id), CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.QuoteWithoutItems, result.Error);
    }

    private ServiceOrder Stored(ServiceOrderStatus status)
    {
        var order = new ServiceOrderBuilder().InStatus(status).Build();
        _serviceOrders.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }
}
