using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Notifications;

namespace Oficina.Infrastructure.Notifications;

public sealed partial class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private const int SendTimeoutMilliseconds = 10_000;

    public async Task SendInvitationAsync(InvitationMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var pickupDirectory = string.IsNullOrWhiteSpace(settings.PickupDirectory) ? null : settings.PickupDirectory;
        if (pickupDirectory is null && string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            LogDeliveryDisabled();
            return;
        }

        var link = InvitationEmail.Link(settings.InvitationUrl, message.Token);
        using var mail = new MailMessage(settings.From, message.RecipientEmail)
        {
            Subject = InvitationEmail.Subject,
            Body = InvitationEmail.Body(message, link),
            IsBodyHtml = true,
        };

        try
        {
            using var client = pickupDirectory is null ? SmtpServerClient(settings) : PickupClient(pickupDirectory);
            await client.SendMailAsync(mail, cancellationToken);
            LogInvitationSent();
        }
        catch (SmtpException exception)
        {
            LogDeliveryFailed(exception.StatusCode);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogPickupDirectoryFailed();
        }
    }

    private static SmtpClient PickupClient(string directory) => new()
    {
        DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
        PickupDirectoryLocation = Directory.CreateDirectory(directory).FullName,
    };

    private static SmtpClient SmtpServerClient(EmailOptions settings) =>
        new(settings.SmtpHost, settings.SmtpPort) { EnableSsl = settings.EnableSsl, Timeout = SendTimeoutMilliseconds };

    [LoggerMessage(Level = LogLevel.Information, Message = "Invitation e-mail sent")]
    private partial void LogInvitationSent();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invitation e-mail not sent: SMTP is not configured")]
    private partial void LogDeliveryDisabled();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invitation e-mail not written: the pickup directory is not available")]
    private partial void LogPickupDirectoryFailed();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invitation e-mail delivery failed with SMTP status {StatusCode}")]
    private partial void LogDeliveryFailed(SmtpStatusCode statusCode);
}
