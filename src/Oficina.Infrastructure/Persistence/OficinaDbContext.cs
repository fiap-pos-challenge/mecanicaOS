using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Common.Interfaces;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Privacy.Entities;
using Oficina.Domain.ServiceOrders.Entities;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence.Conventions;

namespace Oficina.Infrastructure.Persistence;

public sealed class OficinaDbContext(DbContextOptions<OficinaDbContext> options) : DbContext(options)
{
    public const string ServiceOrderNumberSequence = "serviceOrderNumbers";

    public DbSet<User> Users => Set<User>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<Part> Parts => Set<Part>();

    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<SensitiveDataAccess> SensitiveDataAccesses => Set<SensitiveDataAccess>();

    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Conventions.Add(_ => new CamelCaseNamingConvention());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(ServiceOrderNumberSequence).StartsAt(1).IncrementsBy(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OficinaDbContext).Assembly);
        HideSoftDeletedRecords(modelBuilder);
    }

    private static void HideSoftDeletedRecords(ModelBuilder modelBuilder)
    {
        var softDeletableTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => !entityType.IsOwned() && typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType));

        foreach (var entityType in softDeletableTypes)
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var deletedAt = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
            var isNotDeleted = Expression.Equal(deletedAt, Expression.Constant(null, typeof(DateTimeOffset?)));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(isNotDeleted, parameter));
        }
    }
}
