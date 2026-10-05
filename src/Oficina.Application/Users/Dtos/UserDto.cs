using Oficina.Domain.Identity.Enums;

namespace Oficina.Application.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    Role Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
