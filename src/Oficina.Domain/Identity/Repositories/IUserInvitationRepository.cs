using Oficina.Domain.Identity.Entities;

namespace Oficina.Domain.Identity.Repositories;

public interface IUserInvitationRepository
{
    Task<UserInvitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserInvitation>> ListOpenAsync(Guid userId, CancellationToken cancellationToken);

    void Add(UserInvitation invitation);
}
