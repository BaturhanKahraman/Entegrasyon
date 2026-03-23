using Testcontainers.PostgreSql;

namespace Entegrasyon.IntegrationTest.Fixtures;

/// <summary>
/// Shared PostgreSQL container — tum test collection'lar icin tek container.
/// xUnit IAsyncLifetime ile container yasam dongusu yonetilir.
/// </summary>
public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("integration_test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
