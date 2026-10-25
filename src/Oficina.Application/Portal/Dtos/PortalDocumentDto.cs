namespace Oficina.Application.Portal.Dtos;

public sealed record PortalDocumentDto(string Kind, string Code, DateTimeOffset IssuedAt);
