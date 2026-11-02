namespace Oficina.FunctionalTests.Fixtures;

[CollectionDefinition(nameof(DatabaseCollection))]
public sealed class DatabaseCollection : ICollectionFixture<SqlServerFixture>;
