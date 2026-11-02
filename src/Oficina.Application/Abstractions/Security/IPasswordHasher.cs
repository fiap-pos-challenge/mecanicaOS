namespace Oficina.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string DummyHash { get; }

    string Hash(string password);

    bool Verify(string password, string passwordHash);
}
