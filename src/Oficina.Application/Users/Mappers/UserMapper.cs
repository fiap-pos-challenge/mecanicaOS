using Oficina.Application.Users.Dtos;
using Oficina.Domain.Identity.Entities;

namespace Oficina.Application.Users.Mappers;

public static class UserMapper
{
    public static UserDto ToDto(this User user, DateTimeOffset? invitationExpiresAt = null) => new(
        user.Id,
        user.Name,
        user.Email.Value,
        user.Role,
        user.IsActive,
        user.Status,
        user.HasPendingInvitation ? invitationExpiresAt : null,
        user.CreatedAt,
        user.UpdatedAt);
}
