using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Links;

namespace Oficina.Infrastructure.Links;

public sealed class PublicLinks(IOptions<PublicLinksOptions> options) : IPublicLinks
{
    public string Portal(string token) => $"{options.Value.BaseUrl.TrimEnd('/')}/o/{Uri.EscapeDataString(token)}";

    public string DocumentVerification(string code) => $"{options.Value.BaseUrl.TrimEnd('/')}/v/{Uri.EscapeDataString(code)}";
}
