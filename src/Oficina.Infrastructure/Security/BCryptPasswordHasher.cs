using Oficina.Application.Abstractions.Security;

namespace Oficina.Infrastructure.Security;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    private static readonly Lazy<string> LazyDummyHash = new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor));

    public string DummyHash => LazyDummyHash.Value;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
