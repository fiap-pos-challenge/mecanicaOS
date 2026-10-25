using System.Collections.Concurrent;
using Oficina.Application.Abstractions.Notifications;

namespace Oficina.FunctionalTests.Fixtures;

public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<InvitationMessage> _invitations = new();

    public IReadOnlyList<InvitationMessage> Invitations => _invitations.ToArray();

    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyList<EmailMessage> Messages => _messages.ToArray();

    public Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.FromResult(true);
    }

    public Task SendInvitationAsync(InvitationMessage message, CancellationToken cancellationToken)
    {
        _invitations.Enqueue(message);
        return Task.CompletedTask;
    }
}
