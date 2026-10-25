namespace Oficina.Application.Abstractions.Notifications;

public interface IEmailSender
{
    Task SendInvitationAsync(InvitationMessage message, CancellationToken cancellationToken);

    Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
