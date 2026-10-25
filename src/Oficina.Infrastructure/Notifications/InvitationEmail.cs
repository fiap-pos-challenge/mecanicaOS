using System.Globalization;
using System.Net;
using Oficina.Application.Abstractions.Notifications;

namespace Oficina.Infrastructure.Notifications;

public static class InvitationEmail
{
    public const string Subject = "Convite para acessar o mecanicaOS";

    private static readonly TimeSpan BrasiliaOffset = TimeSpan.FromHours(-3);

    public static string Link(string invitationUrl, string token) => $"{invitationUrl}#token={Uri.EscapeDataString(token)}";

    public static string Body(InvitationMessage message, string link)
    {
        var expiresAt = message.ExpiresAt.ToOffset(BrasiliaOffset).ToString("dd/MM/yyyy 'às' HH:mm", CultureInfo.InvariantCulture);
        return $"""
            <p>Olá, {WebUtility.HtmlEncode(message.RecipientName)}.</p>
            <p>Você foi convidado para acessar o mecanicaOS. Crie a sua senha pelo link abaixo. Ninguém, nem o administrador, vê a sua senha.</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Criar minha senha</a></p>
            <p>O link vale até {expiresAt} e só pode ser usado uma vez.</p>
            """;
    }
}
