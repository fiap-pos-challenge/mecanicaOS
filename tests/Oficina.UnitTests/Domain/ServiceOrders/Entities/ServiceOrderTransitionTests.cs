using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Domain.ServiceOrders.Enums;
using Oficina.Domain.ServiceOrders.Errors;
using Oficina.UnitTests.Builders;

namespace Oficina.UnitTests.Domain.ServiceOrders.Entities;

public sealed class ServiceOrderTransitionTests
{
    private static readonly DateTimeOffset Now = ServiceOrderBuilder.OpenedAt.AddDays(1);

    private static readonly Dictionary<string, (ServiceOrderStatus AllowedIn, Func<ServiceOrder, Result> Execute)> Actions = new()
    {
        ["StartDiagnosis"] = (ServiceOrderStatus.Received, order => order.StartDiagnosis(null, Now)),
        ["SendQuote"] = (ServiceOrderStatus.InDiagnosis, order => order.SendQuote(ServiceOrderBuilder.SecondToken, null, Now)),
        ["RegenerateApprovalToken"] = (ServiceOrderStatus.WaitingApproval, order => order.RegenerateApprovalToken(ServiceOrderBuilder.SecondToken)),
        ["ApproveQuote"] = (ServiceOrderStatus.WaitingApproval, order => order.ApproveQuote(null, Now)),
        ["RejectQuote"] = (ServiceOrderStatus.WaitingApproval, order => order.RejectQuote("Cliente achou caro", null, Now)),
        ["Complete"] = (ServiceOrderStatus.InProgress, order => order.Complete(null, Now)),
        ["Deliver"] = (ServiceOrderStatus.Completed, order => order.Deliver(null, Now)),
    };

    private static readonly Dictionary<string, Func<ServiceOrder, Result>> ItemActions = new()
    {
        ["AddServiceItem"] = order => order.AddServiceItem(ServiceOrderBuilder.CreateService(), 1),
        ["AddPartItem"] = order => order.AddPartItem(ServiceOrderBuilder.CreatePart(), Quantity.Create(1m).Value),
        ["RemoveItem"] = order => order.RemoveItem(Guid.CreateVersion7()),
    };

    public static TheoryData<string, ServiceOrderStatus> InvalidTransitions()
    {
        var data = new TheoryData<string, ServiceOrderStatus>();
        foreach (var (action, definition) in Actions)
        {
            foreach (var status in Enum.GetValues<ServiceOrderStatus>().Where(status => status != definition.AllowedIn))
            {
                data.Add(action, status);
            }
        }

        return data;
    }

    public static TheoryData<string, ServiceOrderStatus> ItemChangesOutsideDiagnosis()
    {
        var data = new TheoryData<string, ServiceOrderStatus>();
        foreach (var action in ItemActions.Keys)
        {
            foreach (var status in Enum.GetValues<ServiceOrderStatus>().Where(status => status != ServiceOrderStatus.InDiagnosis))
            {
                data.Add(action, status);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public void Action_InStatusThatDoesNotAllowIt_ReturnsInvalidTransitionAndKeepsStatus(string action, ServiceOrderStatus status)
    {
        var order = new ServiceOrderBuilder().InStatus(status).Build();
        var historyCount = order.StatusHistory.Count;

        var result = Actions[action].Execute(order);

        Assert.Equal(ServiceOrderErrors.InvalidTransition, result.Error);
        Assert.Equal(status, order.Status);
        Assert.Equal(historyCount, order.StatusHistory.Count);
        Assert.Empty(order.DomainEvents);
    }

    [Theory]
    [MemberData(nameof(ItemChangesOutsideDiagnosis))]
    public void ItemChange_OutsideDiagnosis_ReturnsItemsOnlyInDiagnosis(string action, ServiceOrderStatus status)
    {
        var order = new ServiceOrderBuilder().InStatus(status).Build();
        var itemCount = order.ServiceItems.Count + order.PartItems.Count;

        var result = ItemActions[action](order);

        Assert.Equal(ServiceOrderErrors.ItemsOnlyInDiagnosis, result.Error);
        Assert.Equal(itemCount, order.ServiceItems.Count + order.PartItems.Count);
    }
}
