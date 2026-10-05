using Oficina.Application.Users.Dtos;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Riok.Mapperly.Abstractions;

namespace Oficina.Application.Users.Mappers;

[Mapper]
public static partial class UserMapper
{
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.IsActiveAdmin))]
    [MapperIgnoreSource(nameof(User.CanLogin))]
    [MapperIgnoreSource(nameof(User.DomainEvents))]
    [MapperIgnoreSource(nameof(User.DeletedAt))]
    [MapperIgnoreSource(nameof(User.IsDeleted))]
    public static partial UserDto ToDto(this User user);

    private static string EmailToString(Email email) => email.Value;
}
