namespace Oficina.Application.Abstractions.Notifications;

public sealed record EmailMessage(string To, string Subject, string HtmlBody);
