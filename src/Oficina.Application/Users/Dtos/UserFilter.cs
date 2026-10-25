using Oficina.Domain.Identity.Enums;

namespace Oficina.Application.Users.Dtos;

public sealed record UserFilter(Role? Role = null, UserStatus? Status = null);
