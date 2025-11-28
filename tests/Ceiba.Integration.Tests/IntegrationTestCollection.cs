using Xunit;

namespace Ceiba.Integration.Tests;

/// <summary>
/// xUnit collection definition for integration tests.
/// Tests in the same collection run sequentially and share the same fixture instance.
/// This prevents InMemory database conflicts when running tests in parallel.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<CeibaWebApplicationFactory>
{
    // This class has no code, and is never created.
    // Its purpose is simply to be the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}
