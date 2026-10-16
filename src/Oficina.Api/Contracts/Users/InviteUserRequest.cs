using Oficina.Domain.Identity.Enums;

namespace Oficina.Api.Contracts.Users;

public sealed record InviteUserRequest(string Name, string Email, Role Role);
