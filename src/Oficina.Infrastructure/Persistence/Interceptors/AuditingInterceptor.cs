using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Oficina.Domain.Common.Entities;
using Oficina.Domain.Common.Interfaces;

namespace Oficina.Infrastructure.Persistence.Interceptors;

public sealed class AuditingInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditing(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditing(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ApplyAuditing(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        context.ChangeTracker.DetectChanges();
        var now = timeProvider.GetUtcNow();
        var ownedChanges = OwnedEntriesWithChanges(context);
        var ownersWithChangedValues = ownedChanges.Select(FindOwnerId).OfType<Guid>().ToHashSet();
        var softDeletedIds = new HashSet<Guid>();

        foreach (var entry in context.ChangeTracker.Entries<Entity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    ((IAuditable)entry.Entity).MarkCreated(now);
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    ((ISoftDeletable)entry.Entity).MarkDeleted(now);
                    softDeletedIds.Add(entry.Entity.Id);
                    break;
                case EntityState.Modified:
                case EntityState.Unchanged when ownersWithChangedValues.Contains(entry.Entity.Id):
                    ((IAuditable)entry.Entity).MarkUpdated(now);
                    entry.Property(entity => entity.UpdatedAt).IsModified = true;
                    break;
            }
        }

        foreach (var ownedEntry in ownedChanges.Where(entry => entry.State == EntityState.Deleted))
        {
            if (FindOwnerId(ownedEntry) is { } ownerId && softDeletedIds.Contains(ownerId))
            {
                ownedEntry.State = EntityState.Unchanged;
            }
        }
    }

    private static List<EntityEntry> OwnedEntriesWithChanges(DbContext context) =>
        context.ChangeTracker.Entries()
            .Where(entry => entry.Metadata.IsOwned() && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

    private static Guid? FindOwnerId(EntityEntry ownedEntry)
    {
        var ownerKeyProperty = ownedEntry.Metadata.FindOwnership()?.Properties.FirstOrDefault();
        return ownerKeyProperty is null ? null : ownedEntry.Property(ownerKeyProperty.Name).CurrentValue as Guid?;
    }
}
