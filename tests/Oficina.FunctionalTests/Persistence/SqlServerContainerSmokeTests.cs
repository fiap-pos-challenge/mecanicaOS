using Oficina.FunctionalTests.Fixtures;

namespace Oficina.FunctionalTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class SqlServerContainerSmokeTests(SqlServerFixture database)
{
    [Fact]
    public async Task SqlServerContainer_AnswersQueries()
    {
        var rows = await database.QueryAsync("SELECT 1 AS answer");

        Assert.Equal(1, Assert.Single(rows)["answer"]);
    }
}
