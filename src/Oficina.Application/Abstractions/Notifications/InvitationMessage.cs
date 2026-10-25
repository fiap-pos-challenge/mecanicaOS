namespace Oficina.Application.Abstractions.Notifications;

public sealed record InvitationMessage(string RecipientName, string RecipientEmail, string Token, DateTimeOffset ExpiresAt);
