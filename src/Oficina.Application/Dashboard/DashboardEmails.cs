using System.Net;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Quotes.Queries;

namespace Oficina.Application.Dashboard;

public static class DashboardEmails
{
    public static EmailMessage Reminder(string email, EstimateContext context, long number, string link) =>
        new(email, $"Lembrete: orçamento nº {number}",
            $"<p>Olá, {WebUtility.HtmlEncode(context.CustomerName.Split(' ')[0])}! O orçamento do seu {WebUtility.HtmlEncode(context.Vehicle)} ainda aguarda sua resposta.</p><p><a href=\"{link}\">Ver o orçamento</a></p>");

    public static EmailMessage NewDeadline(string email, EstimateContext context, long number) =>
        new(email, $"OS nº {number}: novo prazo",
            $"<p>Olá, {WebUtility.HtmlEncode(context.CustomerName.Split(' ')[0])}! O serviço no seu {WebUtility.HtmlEncode(context.Vehicle)} vai precisar de mais tempo. Entraremos em contato com o novo prazo.</p>");
}
