namespace Oficina.Application.Settings.Dtos;

public sealed record AuditFilter(string? Area, DateOnly? From, DateOnly? To, string? Actor, int Page, int PageSize);
