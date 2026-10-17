using NSubstitute;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Services;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Domain.ServiceOrders.Services;

public sealed class QuoteApprovalPolicyTests
{
    private static readonly DateTimeOffset Now = ServiceOrderBuilder.OpenedAt.AddHours(1);
    private readonly IPartRepository _parts = Substitute.For<IPartRepository>();
    private readonly QuoteApprovalPolicy _policy;

    public QuoteApprovalPolicyTests() => _policy = new QuoteApprovalPolicy(_parts);

    [Fact]
    public async Task Approve_WithEnoughStock_DeductsPartsAndStartsExecution()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
        var part = ServiceOrderBuilder.CreatePart(stock: 5m);
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(part);
        var userId = Guid.CreateVersion7();

        var result = await _policy.ApproveAsync(order, userId, Now, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.InProgress, order.Status);
        Assert.Equal(Now, order.ExecutionStartedAt);
        Assert.Equal(3m, part.StockQuantity);
        var deduction = Assert.Single(part.StockMovements, movement => movement.Type == StockMovementType.Deduction);
        Assert.Equal(order.Id, deduction.ServiceOrderId);
    }

    [Fact]
    public async Task Approve_WithInsufficientStock_ReturnsInsufficientStock()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
        var part = ServiceOrderBuilder.CreatePart(stock: 1m);
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(part);

        var result = await _policy.ApproveAsync(order, null, Now, CancellationToken.None);

        Assert.Equal(InventoryErrors.InsufficientStock, result.Error);
        Assert.Equal(1m, part.StockQuantity);
    }

    [Fact]
    public async Task Approve_PartNotFound_ReturnsInsufficientStock()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
        _parts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Part?)null);

        var result = await _policy.ApproveAsync(order, null, Now, CancellationToken.None);

        Assert.Equal(InventoryErrors.InsufficientStock, result.Error);
    }

    [Theory]
    [InlineData(ServiceOrderStatus.InDiagnosis)]
    [InlineData(ServiceOrderStatus.InProgress)]
    public async Task Approve_OutsideWaitingApproval_ReturnsInvalidTransitionWithoutTouchingStock(ServiceOrderStatus status)
    {
        var order = new ServiceOrderBuilder().InStatus(status).Build();

        var result = await _policy.ApproveAsync(order, null, Now, CancellationToken.None);

        Assert.Equal(ServiceOrderErrors.InvalidTransition, result.Error);
        await _parts.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
