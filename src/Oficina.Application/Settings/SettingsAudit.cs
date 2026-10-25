using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Audit.Entities;
using Oficina.Domain.Audit.Repositories;
using Oficina.Domain.Identity.Repositories;

namespace Oficina.Application.Settings;

public sealed class SettingsAudit(IAuditRepository audit, IUserRepository users, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public const string Discounts = "discounts";
    public const string Payments = "payments";
    public const string Security = "security";

    public async Task RecordAsync(string area, string action, string target, string detail, CancellationToken cancellationToken)
    {
        var actor = currentUser.UserId is { } userId ? await users.GetByIdAsync(userId, cancellationToken) : null;
        audit.Add(AuditEntry.Record(timeProvider.GetUtcNow(), actor?.Id, actor?.Name ?? SettingsMessages.System, area, action, target, detail));
    }
}
