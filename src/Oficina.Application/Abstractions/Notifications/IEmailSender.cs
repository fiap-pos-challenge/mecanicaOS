namespace Oficina.Application.Abstractions.Notifications;

public interface IEmailSender
{
    Task SendInvitationAsync(InvitationMessage message, CancellationToken cancellationToken);
}
