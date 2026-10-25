namespace Oficina.Application.Settings.Models;

public sealed record SendWindowSettings(string From, string To, IReadOnlyList<string> Weekdays);
