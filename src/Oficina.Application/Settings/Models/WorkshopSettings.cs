namespace Oficina.Application.Settings.Models;

public sealed record WorkshopSettings(string Name, string Document, WorkshopAddressSettings Address, string? LogoUrl, IReadOnlyList<BusinessHoursSettings> Hours, WorkshopPlanSettings Plan);
