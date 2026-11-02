namespace Oficina.Api.Authorization;

public static class Policies
{
    public const string Authenticated = nameof(Authenticated);
    public const string AdminOnly = nameof(AdminOnly);
    public const string AdminOrAttendant = nameof(AdminOrAttendant);
    public const string AdminOrMechanic = nameof(AdminOrMechanic);
}
