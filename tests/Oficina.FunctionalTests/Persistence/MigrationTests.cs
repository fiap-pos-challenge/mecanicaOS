using System.Globalization;
using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class MigrationTests(SqlServerFixture database)
{
    private static readonly string[] AuditColumns =
    {
        "createdAt|datetimeoffset|NO",
        "updatedAt|datetimeoffset|YES",
        "deletedAt|datetimeoffset|YES",
    };

    private static readonly Dictionary<string, string[]> ExpectedColumns = new(StringComparer.Ordinal)
    {
        ["users"] = WithAudit(
            "id|uniqueidentifier|NO",
            "name|nvarchar(120)|NO",
            "email|nvarchar(160)|NO",
            "passwordHash|nvarchar(255)|YES",
            "role|varchar(20)|NO",
            "isActive|bit|NO",
            "failedLoginAttempts|int|NO",
            "lockoutEndAt|datetimeoffset|YES"),
        ["customers"] = WithAudit(
            "id|uniqueidentifier|NO",
            "name|nvarchar(160)|NO",
            "document|varchar(14)|NO",
            "documentType|varchar(4)|NO",
            "email|nvarchar(160)|YES",
            "phone|varchar(20)|NO",
            "addressZipCode|char(8)|YES",
            "addressStreet|nvarchar(150)|YES",
            "addressNumber|nvarchar(10)|YES",
            "addressComplement|nvarchar(60)|YES",
            "addressDistrict|nvarchar(80)|YES",
            "addressCity|nvarchar(80)|YES",
            "addressState|char(2)|YES",
            "consentEmail|bit|NO",
            "consentUpdatedAt|datetimeoffset|YES",
            "consentWhatsApp|bit|NO"),
        ["vehicles"] = WithAudit(
            "id|uniqueidentifier|NO",
            "customerId|uniqueidentifier|NO",
            "licensePlate|varchar(7)|NO",
            "brand|nvarchar(60)|NO",
            "mileage|int|NO",
            "model|nvarchar(60)|NO",
            "year|smallint|NO"),
        ["services"] = WithAudit(
            "id|uniqueidentifier|NO",
            "name|nvarchar(120)|NO",
            "description|nvarchar(max)|YES",
            "basePrice|decimal(12,2)|NO",
            "estimatedMinutes|int|NO",
            "commissionPercent|decimal(5,2)|NO",
            "warrantyDays|int|NO",
            "isActive|bit|NO"),
        ["priceChanges"] = new[]
        {
            "id|uniqueidentifier|NO",
            "kind|varchar(10)|NO",
            "itemId|uniqueidentifier|NO",
            "field|varchar(20)|NO",
            "oldValue|decimal(12,2)|YES",
            "newValue|decimal(12,2)|NO",
            "changedBy|uniqueidentifier|YES",
            "changedAt|datetimeoffset|NO",
        },
        ["serviceUsualParts"] = new[]
        {
            "id|uniqueidentifier|NO",
            "serviceId|uniqueidentifier|NO",
            "partId|uniqueidentifier|NO",
            "quantity|decimal(12,3)|NO",
        },
        ["packages"] = WithAudit(
            "id|uniqueidentifier|NO",
            "name|nvarchar(120)|NO",
            "description|nvarchar(500)|YES",
            "price|decimal(12,2)|NO",
            "minMileage|int|YES",
            "maxMileage|int|YES",
            "isActive|bit|NO"),
        ["packageItems"] = new[]
        {
            "id|uniqueidentifier|NO",
            "packageId|uniqueidentifier|NO",
            "kind|varchar(10)|NO",
            "itemId|uniqueidentifier|NO",
            "quantity|decimal(12,3)|NO",
        },
        ["parts"] = WithAudit(
            "id|uniqueidentifier|NO",
            "code|varchar(30)|NO",
            "name|nvarchar(120)|NO",
            "type|varchar(10)|NO",
            "unitOfMeasure|varchar(10)|NO",
            "unitPrice|decimal(12,2)|NO",
            "cost|decimal(12,2)|NO",
            "supplier|nvarchar(120)|YES",
            "barcode|varchar(13)|YES",
            "stockQuantity|decimal(12,3)|NO",
            "minimumStock|decimal(12,3)|NO",
            "isActive|bit|NO",
            "rowVersion|timestamp|NO"),
        ["stockMovements"] = WithAudit(
            "id|uniqueidentifier|NO",
            "partId|uniqueidentifier|NO",
            "serviceOrderId|uniqueidentifier|YES",
            "userId|uniqueidentifier|YES",
            "type|varchar(10)|NO",
            "quantity|decimal(12,3)|NO",
            "balanceAfter|decimal(12,3)|NO",
            "reason|nvarchar(max)|NO"),
        ["serviceOrders"] = WithAudit(
            "id|uniqueidentifier|NO",
            "number|bigint|NO",
            "customerId|uniqueidentifier|NO",
            "vehicleId|uniqueidentifier|NO",
            "status|varchar(30)|NO",
            "problemDescription|nvarchar(max)|NO",
            "totalAmount|decimal(12,2)|NO",
            "executionStartedAt|datetimeoffset|YES",
            "completedAt|datetimeoffset|YES",
            "deliveredAt|datetimeoffset|YES",
            "rowVersion|timestamp|NO"),
        ["serviceItems"] = WithAudit(
            "id|uniqueidentifier|NO",
            "serviceOrderId|uniqueidentifier|NO",
            "serviceId|uniqueidentifier|NO",
            "description|nvarchar(120)|NO",
            "unitPrice|decimal(12,2)|NO",
            "quantity|int|NO"),
        ["partItems"] = WithAudit(
            "id|uniqueidentifier|NO",
            "serviceOrderId|uniqueidentifier|NO",
            "partId|uniqueidentifier|NO",
            "description|nvarchar(120)|NO",
            "unitPrice|decimal(12,2)|NO",
            "quantity|decimal(12,3)|NO"),
        ["quotes"] = WithAudit(
            "id|uniqueidentifier|NO",
            "serviceOrderId|uniqueidentifier|NO",
            "version|int|NO",
            "status|varchar(15)|NO",
            "servicesTotal|decimal(12,2)|NO",
            "partsTotal|decimal(12,2)|NO",
            "total|decimal(12,2)|NO",
            "tokenHash|varchar(64)|YES",
            "sentAt|datetimeoffset|NO",
            "respondedAt|datetimeoffset|YES",
            "rejectionReason|nvarchar(max)|YES"),
        ["serviceOrderStatusHistory"] = WithAudit(
            "id|uniqueidentifier|NO",
            "serviceOrderId|uniqueidentifier|NO",
            "previousStatus|varchar(30)|YES",
            "newStatus|varchar(30)|NO",
            "changedByUserId|uniqueidentifier|YES",
            "note|nvarchar(max)|YES",
            "changedAt|datetimeoffset|NO"),
        ["sensitiveDataAccess"] = new[]
        {
            "id|uniqueidentifier|NO",
            "userId|uniqueidentifier|NO",
            "customerId|uniqueidentifier|NO",
            "field|varchar(10)|NO",
            "reason|varchar(10)|NO",
            "traceId|varchar(64)|YES",
            "occurredAt|datetimeoffset|NO",
        },
        ["userInvitations"] = new[]
        {
            "id|uniqueidentifier|NO",
            "userId|uniqueidentifier|NO",
            "tokenHash|char(64)|NO",
            "createdBy|uniqueidentifier|YES",
            "createdAt|datetimeoffset|NO",
            "expiresAt|datetimeoffset|NO",
            "acceptedAt|datetimeoffset|YES",
            "revokedAt|datetimeoffset|YES",
        },
        ["outboxMessages"] = new[]
        {
            "id|uniqueidentifier|NO",
            "type|varchar(200)|NO",
            "payload|nvarchar(max)|NO",
            "occurredAt|datetimeoffset|NO",
            "processedAt|datetimeoffset|YES",
            "attempts|int|NO",
            "error|nvarchar(2000)|YES",
        },
    };

    [Fact]
    public async Task Database_HasExactlyTheSeventeenModelTables()
    {
        var rows = await database.QueryAsync(
            "SELECT TABLE_NAME AS name FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> '__EFMigrationsHistory'");

        var tables = rows.Select(row => (string)row["name"]!).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(ExpectedColumns.Keys.Order(StringComparer.Ordinal).ToArray(), tables);
    }

    [Fact]
    public async Task EveryTable_HasTheColumnsTypesAndNullabilityOfTheDataModel()
    {
        var rows = await database.QueryAsync(
            """
            SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME <> '__EFMigrationsHistory'
            """);

        var actual = rows
            .GroupBy(row => (string)row["TABLE_NAME"]!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(Describe).Order(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

        foreach (var (table, expected) in ExpectedColumns)
        {
            Assert.Equal(expected.Order(StringComparer.Ordinal).ToArray(), actual[table]);
        }
    }

    [Fact]
    public async Task RowVersion_ExistsOnlyOnPartsAndServiceOrders()
    {
        var rows = await database.QueryAsync(
            "SELECT TABLE_NAME AS name FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME = 'rowVersion' AND DATA_TYPE = 'timestamp'");

        Assert.Equal(new[] { "parts", "serviceOrders" }, rows.Select(row => (string)row["name"]!).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task ServiceOrderNumbersSequence_Exists()
    {
        var rows = await database.QueryAsync(
            "SELECT CAST(start_value AS bigint) AS startValue, CAST(increment AS bigint) AS increment FROM sys.sequences WHERE name = 'serviceOrderNumbers'");

        var sequence = Assert.Single(rows);
        Assert.Equal(1L, sequence["startValue"]);
        Assert.Equal(1L, sequence["increment"]);
    }

    [Theory]
    [InlineData("users", "ux_users_email", "[deletedAt] IS NULL")]
    [InlineData("customers", "ux_customers_document", "[deletedAt] IS NULL")]
    [InlineData("vehicles", "ux_vehicles_licensePlate", "[deletedAt] IS NULL")]
    [InlineData("parts", "ux_parts_code", "[deletedAt] IS NULL")]
    [InlineData("serviceOrders", "ux_serviceOrders_openPerVehicle", "[status]<>'Delivered'")]
    [InlineData("quotes", "ux_quotes_tokenHash", "[tokenHash] IS NOT NULL")]
    public async Task FilteredUniqueIndexes_ExistWithTheirFilters(string table, string index, string filterFragment)
    {
        var row = Assert.Single(await IndexesOf(table, index));

        Assert.True((bool)row["isUnique"]!);
        Assert.Contains(filterFragment, (string)row["filter"]!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("serviceOrders", "ux_serviceOrders_number")]
    [InlineData("quotes", "ux_quotes_serviceOrderId_version")]
    [InlineData("userInvitations", "ux_userInvitations_tokenHash")]
    public async Task UniqueIndexesWithoutFilter_Exist(string table, string index)
    {
        var row = Assert.Single(await IndexesOf(table, index));

        Assert.True((bool)row["isUnique"]!);
        Assert.Null(row["filter"]);
    }

    [Fact]
    public async Task OpenServiceOrderIndex_AlsoIgnoresDeletedOrders()
    {
        var row = Assert.Single(await IndexesOf("serviceOrders", "ux_serviceOrders_openPerVehicle"));

        Assert.Contains("[deletedAt] IS NULL", (string)row["filter"]!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OutboxProcessedAtIndex_Exists()
    {
        Assert.Single(await IndexesOf("outboxMessages", "ix_outboxMessages_processedAt"));
    }

    [Fact]
    public async Task PrimaryKeys_ForeignKeysAndIndexes_UseCamelCaseNames()
    {
        var rows = await database.QueryAsync(
            """
            SELECT name FROM sys.key_constraints WHERE type = 'PK' AND OBJECT_NAME(parent_object_id) <> '__EFMigrationsHistory'
            UNION ALL SELECT name FROM sys.foreign_keys
            UNION ALL SELECT i.name FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
                WHERE i.name IS NOT NULL AND t.name <> '__EFMigrationsHistory'
            """);

        var names = rows.Select(row => (string)row["name"]!).ToArray();

        Assert.NotEmpty(names);
        Assert.All(names, name => Assert.Matches("^(pk|fk|ix|ux)_[a-z][A-Za-z]*(_[a-z][A-Za-z]*)*$", name));
        Assert.Contains("pk_serviceOrderStatusHistory", names);
        Assert.Contains("fk_vehicles_customers_customerId", names);
        Assert.Contains("ix_vehicles_customerId", names);
        Assert.Contains("ix_serviceOrders_vehicleId", names);
    }

    private static string[] WithAudit(params string[] columns) => columns.Concat(AuditColumns).ToArray();

    private static string Describe(IReadOnlyDictionary<string, object?> row)
    {
        var type = (string)row["DATA_TYPE"]!;
        var formattedType = type switch
        {
            "nvarchar" or "varchar" or "char" => row["CHARACTER_MAXIMUM_LENGTH"] is -1
                ? $"{type}(max)"
                : string.Create(CultureInfo.InvariantCulture, $"{type}({row["CHARACTER_MAXIMUM_LENGTH"]})"),
            "decimal" => string.Create(CultureInfo.InvariantCulture, $"decimal({row["NUMERIC_PRECISION"]},{row["NUMERIC_SCALE"]})"),
            _ => type,
        };
        return $"{row["COLUMN_NAME"]}|{formattedType}|{row["IS_NULLABLE"]}";
    }

    private Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> IndexesOf(string table, string index) =>
        database.QueryAsync(
            $"""
            SELECT i.is_unique AS isUnique, i.filter_definition AS filter
            FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
            WHERE t.name = '{table}' AND i.name = '{index}'
            """);
}
