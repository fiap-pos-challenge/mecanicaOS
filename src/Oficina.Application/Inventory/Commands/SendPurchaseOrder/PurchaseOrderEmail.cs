using System.Globalization;
using System.Net;
using System.Text;
using Oficina.Application.Abstractions.Notifications;
using Oficina.Application.Inventory.Dtos;

namespace Oficina.Application.Inventory.Commands.SendPurchaseOrder;

public static class PurchaseOrderEmail
{
    private static readonly CultureInfo Brazil = CultureInfo.GetCultureInfo("pt-BR");

    public static EmailMessage Message(string to, PurchaseOrderDto order)
    {
        var body = new StringBuilder();
        body.Append(CultureInfo.InvariantCulture, $"<p>Olá, {WebUtility.HtmlEncode(order.SupplierName)}.</p><p>Segue o pedido de compra nº {order.Number}:</p><ul>");
        foreach (var line in order.Lines)
        {
            body.Append(CultureInfo.InvariantCulture, $"<li>{WebUtility.HtmlEncode(line.Code)} · {WebUtility.HtmlEncode(line.Name)} — {line.Quantity.ToString("0.###", Brazil)} × {line.UnitCost.ToString("C", Brazil)}</li>");
        }

        body.Append(CultureInfo.InvariantCulture, $"</ul><p>Total: {order.Total.ToString("C", Brazil)}</p>");
        return new EmailMessage(to, $"Pedido de compra nº {order.Number}", body.ToString());
    }
}
