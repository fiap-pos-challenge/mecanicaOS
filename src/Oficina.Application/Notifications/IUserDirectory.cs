namespace Oficina.Application.Notifications;

public interface IUserDirectory
{
    Task<IReadOnlyList<(Guid UserId, string Email)>> ActiveEmailsAsync(CancellationToken cancellationToken);
}
