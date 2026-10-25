namespace Oficina.Application.Abstractions.Security;

public interface ISecretProtector
{
    string Protect(string secret);

    string? Unprotect(string protectedSecret);

    bool IsProtected(string value);
}
