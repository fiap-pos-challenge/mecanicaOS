using Oficina.Domain.Identity.Enums;

namespace Oficina.Api.Contracts.Users;

public sealed record CreateUserRequest(string Name, string Email, string Password, Role Role);
