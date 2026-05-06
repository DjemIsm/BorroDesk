namespace BorroDesk.Api.Tests.Infrastructure;

public abstract class IntegrationTestBase(CustomWebApplicationFactory factory) : IAsyncLifetime
{
    protected CustomWebApplicationFactory Factory { get; } = factory;

    protected AuthenticatedClientFactory ClientFactory { get; } = new(factory);

    protected TestDataHelper TestData { get; } = new(factory);

    public Task InitializeAsync()
    {
        return Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
