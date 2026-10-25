namespace Oficina.Application.Settings.Dtos;

public sealed record WorkshopInput(string Name, string Document, Models.WorkshopAddressSettings Address, IReadOnlyList<Models.BusinessHoursSettings> Hours);
