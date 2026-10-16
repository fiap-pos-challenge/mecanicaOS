using System.Collections.Concurrent;
using Oficina.Application.Abstractions.Notifications;

namespace Oficina.FunctionalTests.Fixtures;

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<InvitationMessage> _invitations = new();

    public IReadOnlyList<InvitationMessage> Invitations => _invitations.ToArray();

    public Task SendInvitationAsync(InvitationMessage message, CancellationToken cancellationToken)
    {
        _invitations.Enqueue(message);
        return Task.CompletedTask;
    }
}
