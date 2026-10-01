using NSubstitute;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.ServiceOrders.EventHandlers;
using Oficina.Domain.ServiceOrders.Events;

namespace Oficina.UnitTests.Application.ServiceOrders.EventHandlers;

public sealed class NotifyCustomerOnServiceOrderCompletedTests
{
    [Fact]
    public async Task HandleAsync_NotifiesTheCustomerWithTheServiceOrderNumber()
    {
        var notifier = Substitute.For<INotifier>();
        var handler = new NotifyCustomerOnServiceOrderCompleted(notifier);
        var completed = new ServiceOrderCompleted(Guid.CreateVersion7(), 1234, Guid.CreateVersion7(), DateTimeOffset.MinValue);

        await handler.HandleAsync(completed, CancellationToken.None);

        await notifier.Received(1).NotifyServiceOrderCompletedAsync(1234, Arg.Any<CancellationToken>());
    }
}
