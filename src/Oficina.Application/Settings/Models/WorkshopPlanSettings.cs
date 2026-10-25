namespace Oficina.Application.Settings.Models;

public sealed record WorkshopPlanSettings(string Name, DateTimeOffset RenewsAt, int Users, int MaxUsers);
