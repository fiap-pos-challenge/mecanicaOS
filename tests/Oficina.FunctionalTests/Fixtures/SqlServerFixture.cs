using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oficina.Infrastructure.Persistence;
using Oficina.Infrastructure.Persistence.Interceptors;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace Oficina.FunctionalTests.Fixtures;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string DockerRequiredMessage =
        "Os testes funcionais precisam do Docker em execução (o Testcontainers sobe um SQL Server temporário). Abra o Docker Desktop e rode os testes de novo.";

    private MsSqlContainer? _container;
    private Respawner? _respawner;

    public string ConnectionString => new SqlConnectionStringBuilder(Container.GetConnectionString()) { InitialCatalog = "oficinaTests" }.ConnectionString;

    private MsSqlContainer Container => _container ?? throw new InvalidOperationException(DockerRequiredMessage);

    public async ValueTask InitializeAsync()
    {
        try
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _container.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (DockerUnavailableException exception)
        {
            throw new InvalidOperationException(DockerRequiredMessage, exception);
        }

        await using var context = CreateContext(TimeProvider.System);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = new Table[] { "__EFMigrationsHistory" },
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public OficinaDbContext CreateContext(TimeProvider timeProvider)
    {
        var options = new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new AuditingInterceptor(timeProvider))
            .Options;
        return new OficinaDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        if (_respawner is { } respawner)
        {
            await respawner.ResetAsync(connection);
        }
    }

    public async Task<int> CountRowsAsync(string table, Guid id)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new SqlCommand($"SELECT COUNT(*) FROM [{table}] WHERE [id] = @id", connection);
        command.Parameters.AddWithValue("@id", id);
        return (int)(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? 0);
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> QueryAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.Ordinal);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
            }

            rows.Add(row);
        }

        return rows;
    }
}
