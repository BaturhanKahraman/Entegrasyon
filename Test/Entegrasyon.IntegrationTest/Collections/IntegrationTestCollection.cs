using Entegrasyon.IntegrationTest.Fixtures;

namespace Entegrasyon.IntegrationTest.Collections;

/// <summary>
/// xUnit collection definition — tek PostgreSQL container tum testler tarafindan paylasilir.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "IntegrationTest";
}
