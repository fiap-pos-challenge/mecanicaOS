using FluentValidation;
using Oficina.Application.Common.Validation;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Errors;

namespace Oficina.Application.Users.Validation;

public static class UserRules
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 64;

    public static IRuleBuilderOptions<T, string> ValidUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(name => name?.Trim().Length is >= User.NameMinLength and <= User.NameMaxLength)
            .WithError(IdentityErrors.InvalidName);

    public static IRuleBuilderOptionsConditions<T, string> ValidEmail<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule.Cascade(CascadeMode.Stop).NotEmpty().WithError(CommonErrors.InvalidEmail).MustCreate(Email.Create);

    public static IRuleBuilderOptions<T, Role> ValidRole<T>(this IRuleBuilder<T, Role> rule) =>
        rule.IsInEnum().WithError(IdentityErrors.InvalidRole);

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(IsStrong).WithError(IdentityErrors.WeakPassword);

    private static bool IsStrong(string? password) =>
        password is { Length: >= PasswordMinLength and <= PasswordMaxLength }
        && password.Any(char.IsAsciiLetterUpper)
        && password.Any(char.IsAsciiLetterLower)
        && password.Any(char.IsAsciiDigit)
        && password.Any(character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character));
}
