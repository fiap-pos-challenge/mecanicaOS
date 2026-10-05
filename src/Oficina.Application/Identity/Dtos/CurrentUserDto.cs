using Oficina.Domain.Identity.Enums;

namespace Oficina.Application.Identity.Dtos;

public sealed record CurrentUserDto(Guid Id, string Name, string Email, Role Role);
