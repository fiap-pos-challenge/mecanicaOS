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
using Oficina.Domain.Team.Entities;
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

    public DbSet<Package> Packages => Set<Package>();

    public DbSet<PriceChange> PriceChanges => Set<PriceChange>();

    public DbSet<Part> Parts => Set<Part>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<StockCount> StockCounts => Set<StockCount>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<ServiceOrderPhoto> ServiceOrderPhotos => Set<ServiceOrderPhoto>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<SensitiveDataAccess> SensitiveDataAccesses => Set<SensitiveDataAccess>();

    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<TeamProfile> TeamProfiles => Set<TeamProfile>();

    public DbSet<Domain.Quotes.Entities.Estimate> Estimates => Set<Domain.Quotes.Entities.Estimate>();

    public DbSet<Domain.Settings.Entities.SettingsDocument> SettingsDocuments => Set<Domain.Settings.Entities.SettingsDocument>();

    public DbSet<Domain.Portal.Entities.PortalSession> PortalSessions => Set<Domain.Portal.Entities.PortalSession>();

    public DbSet<Domain.Portal.Entities.PortalCharge> PortalCharges => Set<Domain.Portal.Entities.PortalCharge>();

    public DbSet<Domain.Documents.Entities.IssuedDocument> IssuedDocuments => Set<Domain.Documents.Entities.IssuedDocument>();

    public DbSet<Domain.Audit.Entities.AuditEntry> AuditEntries => Set<Domain.Audit.Entities.AuditEntry>();

    public DbSet<Application.Jobs.Models.BackgroundJob> BackgroundJobs => Set<Application.Jobs.Models.BackgroundJob>();

    public DbSet<Domain.Dashboard.DashboardAction> DashboardActions => Set<Domain.Dashboard.DashboardAction>();

    public DbSet<Domain.Reports.SavedReport> SavedReports => Set<Domain.Reports.SavedReport>();

    public DbSet<Domain.Notifications.Notification> Notifications => Set<Domain.Notifications.Notification>();

    public DbSet<Domain.Notifications.NotificationRead> NotificationReads => Set<Domain.Notifications.NotificationRead>();

    public DbSet<Domain.Automations.AutomationRule> AutomationRules => Set<Domain.Automations.AutomationRule>();

    public DbSet<Domain.Automations.AutomationExecution> AutomationExecutions => Set<Domain.Automations.AutomationExecution>();

    public DbSet<TwoFactorChallenge> TwoFactorChallenges => Set<TwoFactorChallenge>();

    public DbSet<Reauthentication> Reauthentications => Set<Reauthentication>();

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
