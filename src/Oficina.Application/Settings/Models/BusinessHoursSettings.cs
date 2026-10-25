namespace Oficina.Application.Settings.Models;

public sealed record BusinessHoursSettings(string Weekday, bool Closed, string Opens, string Closes);
