using Testcontainers.LocalStack;
using Xunit;

namespace Extensions.AWS.IntegrationTests;

/// <summary>
/// Shared LocalStack container fixture for all integration tests.
/// Uses Testcontainers to automatically manage container lifecycle.
/// </summary>
public class LocalStackFixture : IAsyncLifetime
{
    private readonly LocalStackContainer _container;

    public LocalStackFixture()
    {
        _container = new LocalStackBuilder()
            .WithImage("localstack/localstack:3.0")
            .Build();
    }

    public string Endpoint => _container.GetConnectionString();
    
    public string Region => "us-east-1";
    
    public string AccessKey => "test";
    
    public string SecretKey => "test";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

/// <summary>
/// Collection definition for sharing LocalStack container across tests.
/// </summary>
[CollectionDefinition("LocalStack")]
public class LocalStackCollection : ICollectionFixture<LocalStackFixture>
{
}
