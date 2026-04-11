using Entegrasyon.IntegrationTest.Fixtures;

namespace Entegrasyon.IntegrationTest.Collections;

/// <summary>
/// xUnit collection definition — tek PostgreSQL container ve tek WireMock in-process
/// server tum integration testler tarafindan paylasilir. WireMockFixture marketplace
/// HTTP client'larini mock'layan in-process server; her test class'i ResetAll() ile
/// kendi stub'larini kurar.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection
    : ICollectionFixture<PostgreSqlFixture>,
      ICollectionFixture<WireMockFixture>
{
    public const string Name = "IntegrationTest";
}
