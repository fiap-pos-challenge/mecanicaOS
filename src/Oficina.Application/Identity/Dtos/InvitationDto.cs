using Oficina.Domain.Identity.Enums;

namespace Oficina.Application.Identity.Dtos;

public sealed record InvitationDto(string Name, string MaskedEmail, Role Role, DateTimeOffset ExpiresAt);
