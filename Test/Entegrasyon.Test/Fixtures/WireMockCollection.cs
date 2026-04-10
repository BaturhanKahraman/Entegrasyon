namespace Entegrasyon.Test.Fixtures;

/// <summary>
/// xUnit collection definition — [Collection("WireMock")] attribute'u ile isaretlenmis
/// test class'lari ayni WireMockFixture instance'ini paylasir. xUnit ayni collection'daki
/// class'lari SERIALIZE eder (paralel degil), bu sayede ResetAll() race condition olmaz.
///
/// Kullanim:
///   [Collection(WireMockCollection.Name)]
///   public class TrendyolApiClientTests(WireMockFixture wm) { ... }
/// </summary>
[CollectionDefinition(Name)]
public sealed class WireMockCollection : ICollectionFixture<WireMockFixture>
{
    public const string Name = "WireMock";

    // Bu sinif kod barindirmaz — sadece ICollectionFixture<T> marker interface'i.
    // xUnit bu sinifi tarar ve CollectionDefinition attribute'u ile eslesen
    // test class'larina ayni WireMockFixture instance'ini inject eder.
}
