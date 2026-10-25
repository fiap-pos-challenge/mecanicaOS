using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Oficina.Infrastructure.Persistence.Conventions;

public sealed class CamelCaseNamingConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            if (entityType.GetTableName() is not { } table || entityType.IsMappedToJson())
            {
                continue;
            }

            NameColumns(entityType);

            if (SharesOwnerTable(entityType))
            {
                continue;
            }

            entityType.FindPrimaryKey()?.SetName($"pk_{table}");

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var principalTable = foreignKey.PrincipalEntityType.GetTableName();
                foreignKey.SetConstraintName($"fk_{table}_{principalTable}_{JoinColumns(foreignKey.Properties)}");
            }

            foreach (var index in entityType.GetIndexes().Where(index => index.GetDatabaseNameConfigurationSource() != ConfigurationSource.Explicit))
            {
                index.SetDatabaseName($"ix_{table}_{JoinColumns(index.Properties)}");
            }
        }
    }

    private static void NameColumns(IConventionEntityType entityType)
    {
        foreach (var property in entityType.GetDeclaredProperties())
        {
            var isOwnedKey = SharesOwnerTable(entityType) && property.IsPrimaryKey();
            if (isOwnedKey || property.GetColumnNameConfigurationSource() == ConfigurationSource.Explicit)
            {
                continue;
            }

            property.SetColumnName(ToCamelCase(property.Name));
        }
    }

    private static bool SharesOwnerTable(IConventionEntityType entityType) =>
        entityType.FindOwnership() is { } ownership && ownership.PrincipalEntityType.GetTableName() == entityType.GetTableName();

    private static string JoinColumns(IEnumerable<IConventionProperty> properties) =>
        string.Join("_", properties.Select(property => property.GetColumnName()));

    private static string ToCamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}
