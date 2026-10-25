namespace Oficina.Api.Authorization;

public static class SessionCookies
{
    public const string RefreshCookieName = "oficina_refresh";
    public const string CookiePath = "/api/auth";
    public const string ReauthenticationHeader = "X-Reauthentication";

    public static string? ReadRefresh(HttpRequest request) => request.Cookies[RefreshCookieName];

    public static void WriteRefresh(HttpResponse response, string token, DateTimeOffset expiresAt) =>
        response.Cookies.Append(RefreshCookieName, token, Options(expiresAt));

    public static void ClearRefresh(HttpResponse response) =>
        response.Cookies.Delete(RefreshCookieName, Options(null));

    public static string? ReadDevice(HttpRequest request)
    {
        var agent = request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(agent) ? null : agent;
    }

    private static CookieOptions Options(DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expiresAt,
        IsEssential = true,
    };
}
