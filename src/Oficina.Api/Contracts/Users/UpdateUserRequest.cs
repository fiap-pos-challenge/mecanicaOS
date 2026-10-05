using Oficina.Domain.Identity.Enums;

namespace Oficina.Api.Contracts.Users;

public sealed record UpdateUserRequest(string Name, Role Role);
