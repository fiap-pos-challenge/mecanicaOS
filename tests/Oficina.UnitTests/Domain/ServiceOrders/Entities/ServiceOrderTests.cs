using Oficina.Domain.Catalog.Errors;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Inventory.Errors;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.Domain.ServiceOrders.Events;
using Oficina.Domain.ServiceOrders.ValueObjects;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Domain.ServiceOrders.Entities;

public sealed class ServiceOrderTests
{
    private static readonly DateTimeOffset Now = ServiceOrderBuilder.OpenedAt.AddDays(1);
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly ServiceOrderNumber Number = ServiceOrderNumber.Create(1001).Value;

    [Fact]
    public void Open_WithValidData_StartsReceivedWithHistoryAndEvent()
    {
        var customerId = Guid.CreateVersion7();
        var vehicleId = Guid.CreateVersion7();

        var result = ServiceOrder.Open(Number, customerId, vehicleId, " Barulho ao frear ", UserId, Now);

        Assert.True(result.IsSuccess);
        var order = result.Value;
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(Number, order.Number);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(vehicleId, order.VehicleId);
        Assert.Equal("Barulho ao frear", order.ProblemDescription);
        Assert.Equal(ServiceOrderStatus.Received, order.Status);
        Assert.Equal(Money.Zero, order.TotalAmount);
        Assert.True(order.IsOpen);
        var history = Assert.Single(order.StatusHistory);
        Assert.Null(history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.Received, history.NewStatus);
        Assert.Equal(UserId, history.ChangedByUserId);
        Assert.Equal(Now, history.ChangedAt);
        var opened = Assert.IsType<ServiceOrderOpened>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, opened.ServiceOrderId);
        Assert.Equal(1001, opened.Number);
    }

    [Fact]
    public void Open_WithoutCustomer_ReturnsCustomerRequired()
    {
        Assert.Equal(ServiceOrderErrors.CustomerRequired, ServiceOrder.Open(Number, Guid.Empty, Guid.CreateVersion7(), "Barulho", null, Now).Error);
    }

    [Fact]
    public void Open_WithoutVehicle_ReturnsVehicleRequired()
    {
        Assert.Equal(ServiceOrderErrors.VehicleRequired, ServiceOrder.Open(Number, Guid.CreateVersion7(), Guid.Empty, "Barulho", null, Now).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void Open_WithTooShortProblemDescription_ReturnsInvalidProblemDescription(string description)
    {
        Assert.Equal(
            ServiceOrderErrors.InvalidProblemDescription,
            ServiceOrder.Open(Number, Guid.CreateVersion7(), Guid.CreateVersion7(), description, null, Now).Error);
    }

    [Fact]
    public void StartDiagnosis_FromReceived_MovesToInDiagnosisAndRecordsHistory()
    {
        var order = new ServiceOrderBuilder().Build();

        var result = order.StartDiagnosis(UserId, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.InDiagnosis, order.Status);
        var history = order.StatusHistory.Last();
        Assert.Equal(ServiceOrderStatus.Received, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.InDiagnosis, history.NewStatus);
        Assert.Equal(UserId, history.ChangedByUserId);
    }

    [Fact]
    public void AddServiceItem_InDiagnosis_CopiesNameAndPriceFromCatalog()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();
        var service = ServiceOrderBuilder.CreateService(price: 120m);

        var result = order.AddServiceItem(service, 2);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(order.ServiceItems);
        Assert.Equal(service.Id, item.ServiceId);
        Assert.Equal(order.Id, item.ServiceOrderId);
        Assert.Equal("Troca de óleo", item.Description);
        Assert.Equal(120m, item.UnitPrice.Amount);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(240m, item.Total.Amount);
    }

    [Fact]
    public void AddServiceItem_PriceChangesLater_ItemKeepsCopiedPrice()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();
        var service = ServiceOrderBuilder.CreateService(price: 120m);
        order.AddServiceItem(service, 1);

        service.ChangePrice(Money.Create(999m).Value);

        Assert.Equal(120m, order.ServiceItems.Single().UnitPrice.Amount);
    }

    [Fact]
    public void AddServiceItem_InactiveService_ReturnsServiceInactive()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();

        var result = order.AddServiceItem(ServiceOrderBuilder.CreateService(active: false), 1);

        Assert.Equal(CatalogErrors.ServiceInactive, result.Error);
        Assert.Empty(order.ServiceItems);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddServiceItem_NonPositiveQuantity_ReturnsInvalidServiceQuantity(int quantity)
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();

        Assert.Equal(ServiceOrderErrors.InvalidServiceQuantity, order.AddServiceItem(ServiceOrderBuilder.CreateService(), quantity).Error);
    }

    [Fact]
    public void AddPartItem_WithStock_CopiesNameAndPrice()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();
        var part = ServiceOrderBuilder.CreatePart(price: 30m, stock: 5m);

        var result = order.AddPartItem(part, Quantity.Create(1.5m).Value);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(order.PartItems);
        Assert.Equal(part.Id, item.PartId);
        Assert.Equal("Filtro de óleo", item.Description);
        Assert.Equal(30m, item.UnitPrice.Amount);
        Assert.Equal(1.5m, item.Quantity.Value);
        Assert.Equal(45m, item.Total.Amount);
    }

    [Fact]
    public void AddPartItem_WithoutEnoughStock_ReturnsPartUnavailable()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();

        var result = order.AddPartItem(ServiceOrderBuilder.CreatePart(stock: 1m), Quantity.Create(2m).Value);

        Assert.Equal(InventoryErrors.PartUnavailable, result.Error);
        Assert.Empty(order.PartItems);
    }

    [Fact]
    public void AddPartItem_InactivePart_ReturnsPartInactive()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();

        var result = order.AddPartItem(ServiceOrderBuilder.CreatePart(active: false), Quantity.Create(1m).Value);

        Assert.Equal(InventoryErrors.PartInactive, result.Error);
    }

    [Fact]
    public void RemoveItem_ExistingServiceAndPartItems_RemovesThem()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();
        order.AddServiceItem(ServiceOrderBuilder.CreateService(), 1);
        order.AddPartItem(ServiceOrderBuilder.CreatePart(), Quantity.Create(1m).Value);

        Assert.True(order.RemoveItem(order.ServiceItems.Single().Id).IsSuccess);
        Assert.True(order.RemoveItem(order.PartItems.Single().Id).IsSuccess);

        Assert.Empty(order.ServiceItems);
        Assert.Empty(order.PartItems);
    }

    [Fact]
    public void RemoveItem_UnknownItem_ReturnsItemNotFound()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();

        Assert.Equal(ServiceOrderErrors.ItemNotFound, order.RemoveItem(Guid.CreateVersion7()).Error);
    }

    [Fact]
    public void SendQuote_WithoutItems_ReturnsQuoteWithoutItems()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();

        var result = order.SendQuote(ServiceOrderBuilder.FirstToken, UserId, Now);

        Assert.Equal(ServiceOrderErrors.QuoteWithoutItems, result.Error);
        Assert.Equal(ServiceOrderStatus.InDiagnosis, order.Status);
        Assert.Empty(order.Quotes);
    }

    [Fact]
    public void SendQuote_WithItems_CalculatesTotalsAndWaitsForApproval()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InDiagnosis).Build();
        order.AddServiceItem(ServiceOrderBuilder.CreateService(price: 100.10m), 2);
        order.AddServiceItem(ServiceOrderBuilder.CreateService(price: 50m), 1);
        order.AddPartItem(ServiceOrderBuilder.CreatePart(price: 10.01m), Quantity.Create(1.5m).Value);

        var result = order.SendQuote(ServiceOrderBuilder.FirstToken, UserId, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.WaitingApproval, order.Status);
        var quote = Assert.Single(order.Quotes);
        Assert.Equal(1, quote.Version);
        Assert.Equal(QuoteStatus.Pending, quote.Status);
        Assert.Equal(250.20m, quote.ServicesTotal.Amount);
        Assert.Equal(15.02m, quote.PartsTotal.Amount);
        Assert.Equal(265.22m, quote.Total.Amount);
        Assert.Equal(265.22m, order.TotalAmount.Amount);
        Assert.Equal(ServiceOrderBuilder.FirstToken, quote.ApprovalToken);
        Assert.Equal(Now, quote.SentAt);
        Assert.Equal(quote, order.CurrentQuote);
        var sent = Assert.Single(order.DomainEvents.OfType<QuoteSent>());
        Assert.Equal(1, sent.QuoteVersion);
        Assert.Equal(265.22m, sent.Total);
    }

    [Fact]
    public void MatchesApprovalToken_OnlyForCurrentPendingToken()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();

        Assert.True(order.MatchesApprovalToken(ServiceOrderBuilder.FirstToken));
        Assert.False(order.MatchesApprovalToken(ServiceOrderBuilder.SecondToken));
    }

    [Fact]
    public void RegenerateApprovalToken_InvalidatesThePreviousToken()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();

        var result = order.RegenerateApprovalToken(ServiceOrderBuilder.SecondToken);

        Assert.True(result.IsSuccess);
        Assert.False(order.MatchesApprovalToken(ServiceOrderBuilder.FirstToken));
        Assert.True(order.MatchesApprovalToken(ServiceOrderBuilder.SecondToken));
        Assert.Single(order.Quotes);
    }

    [Fact]
    public void ApproveQuote_StartsExecutionConsumesTokenAndRaisesEventWithParts()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
        var partItem = order.PartItems.Single();

        var result = order.ApproveQuote(UserId, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.InProgress, order.Status);
        Assert.Equal(Now, order.ExecutionStartedAt);
        var quote = order.CurrentQuote!;
        Assert.Equal(QuoteStatus.Approved, quote.Status);
        Assert.Equal(Now, quote.RespondedAt);
        Assert.Null(quote.ApprovalToken);
        Assert.False(order.MatchesApprovalToken(ServiceOrderBuilder.FirstToken));
        var approved = Assert.Single(order.DomainEvents.OfType<QuoteApproved>());
        var approvedPart = Assert.Single(approved.Parts);
        Assert.Equal(partItem.PartId, approvedPart.PartId);
        Assert.Equal(partItem.Quantity.Value, approvedPart.Quantity);
    }

    [Fact]
    public void ApproveQuote_Twice_SecondAttemptIsRejected()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
        order.ApproveQuote(null, Now);

        Assert.Equal(ServiceOrderErrors.InvalidTransition, order.ApproveQuote(null, Now).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectQuote_WithoutReason_ReturnsRejectionReasonRequired(string reason)
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();

        var result = order.RejectQuote(reason, null, Now);

        Assert.Equal(ServiceOrderErrors.RejectionReasonRequired, result.Error);
        Assert.Equal(ServiceOrderStatus.WaitingApproval, order.Status);
    }

    [Fact]
    public void RejectQuote_WithReason_ReturnsToDiagnosisAndKeepsReasonInQuoteAndHistory()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();

        var result = order.RejectQuote(" Achou caro ", UserId, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.InDiagnosis, order.Status);
        var quote = order.CurrentQuote!;
        Assert.Equal(QuoteStatus.Rejected, quote.Status);
        Assert.Equal("Achou caro", quote.RejectionReason);
        Assert.Null(quote.ApprovalToken);
        Assert.Equal("Achou caro", order.StatusHistory.Last().Note);
        Assert.Single(order.DomainEvents.OfType<QuoteRejected>());
    }

    [Fact]
    public void SendQuote_AfterRejection_CreatesNextVersion()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.WaitingApproval).Build();
        order.RejectQuote("Achou caro", null, Now);
        order.RemoveItem(order.PartItems.Single().Id);

        var result = order.SendQuote(ServiceOrderBuilder.SecondToken, null, Now.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, order.Quotes.Count);
        Assert.Equal(2, order.CurrentQuote!.Version);
        Assert.Equal(QuoteStatus.Pending, order.CurrentQuote.Status);
        Assert.Equal(100m, order.TotalAmount.Amount);
    }

    [Fact]
    public void Complete_FromInProgress_RecordsCompletionTimeAndRaisesEvent()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InProgress).Build();
        var completedAt = order.ExecutionStartedAt!.Value.AddHours(2).AddMinutes(30);

        var result = order.Complete(UserId, completedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.Completed, order.Status);
        Assert.Equal(completedAt, order.CompletedAt);
        Assert.Equal(TimeSpan.FromMinutes(150), order.ExecutionTime);
        var completed = Assert.Single(order.DomainEvents.OfType<ServiceOrderCompleted>());
        Assert.Equal(order.CustomerId, completed.CustomerId);
    }

    [Fact]
    public void ExecutionTime_BeforeCompletion_IsNull()
    {
        Assert.Null(new ServiceOrderBuilder().InStatus(ServiceOrderStatus.InProgress).Build().ExecutionTime);
    }

    [Fact]
    public void Deliver_FromCompleted_ClosesTheOrder()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.Completed).Build();

        var result = order.Deliver(UserId, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ServiceOrderStatus.Delivered, order.Status);
        Assert.Equal(Now, order.DeliveredAt);
        Assert.False(order.IsOpen);
    }

    [Fact]
    public void FullFlow_RecordsOneHistoryEntryPerStatus()
    {
        var order = new ServiceOrderBuilder().InStatus(ServiceOrderStatus.Delivered).Build();

        var statuses = order.StatusHistory.Select(history => history.NewStatus).ToArray();

        Assert.Equal(Enum.GetValues<ServiceOrderStatus>(), statuses);
    }
}
