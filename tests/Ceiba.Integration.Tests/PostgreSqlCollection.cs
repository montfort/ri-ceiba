namespace Ceiba.Integration.Tests;

/// <summary>
/// Collection definition for PostgreSQL tests.
/// Ensures tests using PostgreSQL run sequentially to avoid database conflicts.
/// </summary>
[CollectionDefinition("PostgreSQL")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlWebApplicationFactory>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
