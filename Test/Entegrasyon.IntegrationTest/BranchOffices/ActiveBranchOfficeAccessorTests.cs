using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.User;
using Entegrasyon.IntegrationTest.Fixtures;
using Entegrasyon.MVC.Infrastructure.BranchOffices;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Entegrasyon.IntegrationTest.BranchOffices;

/// <summary>
/// Faz 2: ActiveBranchOfficeAccessor integration testleri — gerçek PostgreSQL + fake ISession.
/// Öncelik sırası: LastSelected (remember=true) → DefaultBranchOffice → HQ.
/// Stale reset + SwitchAsync erişim kontrolü + persist davranışı.
/// </summary>
[Trait("Category", "Integration")]
public class ActiveBranchOfficeAccessorTests : IntegrationTestBase
{
    private int _hqId;
    private int _officeAId;
    private int _officeBId;
    private Guid _userId;

    public ActiveBranchOfficeAccessorTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        using var db = CreateDbContext();

        // Seed HQ'nun varlığını doğrula (migration seed'den gelir)
        _hqId = await db.BranchOffices
            .Where(b => b.IsHeadquarters)
            .Select(b => b.Id)
            .FirstAsync();

        // Ekstra iki test ofisi
        var officeA = new BranchOffice
        {
            Name = "Şube A",
            NormalizedName = "SUBE A",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var officeB = new BranchOffice
        {
            Name = "Şube B",
            NormalizedName = "SUBE B",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.BranchOffices.AddRange(officeA, officeB);
        await db.SaveChangesAsync();

        _officeAId = officeA.Id;
        _officeBId = officeB.Id;

        // Test kullanıcısı — DefaultBranchOfficeId = Şube A, junction'da Şube A
        _userId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = _userId,
            UserName = "accessor-test-user",
            NormalizedUserName = "ACCESSOR-TEST-USER",
            IsActive = true,
            DefaultBranchOfficeId = _officeAId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.UserBranchOffices.Add(new UserBranchOffice
        {
            UserId = _userId,
            BranchOfficeId = _officeAId,
            AssignedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private (ActiveBranchOfficeAccessor Accessor, FakeHttpContextAccessor HttpCtx) CreateAccessor()
    {
        var httpCtx = new FakeHttpContextAccessor();
        var contextFactory = Services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var accessor = new ActiveBranchOfficeAccessor(
            httpCtx,
            contextFactory,
            NullLogger<ActiveBranchOfficeAccessor>.Instance);
        return (accessor, httpCtx);
    }

    [Fact]
    public async Task Resolve_falls_back_to_DefaultBranchOffice_when_no_remember()
    {
        var (accessor, httpCtx) = CreateAccessor();

        var result = await accessor.ResolveAndStoreAsync(_userId);

        result.Should().Be(_officeAId, "kullanıcının DefaultBranchOfficeId'si Şube A");
        httpCtx.HttpContext!.Session.GetInt32("ActiveBranchOfficeId").Should().Be(_officeAId);
    }

    [Fact]
    public async Task Resolve_uses_LastSelected_when_remember_true()
    {
        // Arrange: kullanıcıya LastSelected = Şube B, remember=true ayarla
        using (var db = CreateDbContext())
        {
            await db.Users
                .Where(u => u.Id == _userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.LastSelectedBranchOfficeId, _officeBId)
                    .SetProperty(x => x.RememberLastBranchOffice, true));
            // Junction'a Şube B de ekle ki kullanıcı erişebilsin
            db.UserBranchOffices.Add(new UserBranchOffice
            {
                UserId = _userId,
                BranchOfficeId = _officeBId,
                AssignedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var (accessor, _) = CreateAccessor();

        var result = await accessor.ResolveAndStoreAsync(_userId);

        result.Should().Be(_officeBId, "remember=true ve LastSelected=Şube B");
    }

    [Fact]
    public async Task Resolve_falls_through_to_default_when_LastSelected_is_soft_deleted()
    {
        // Arrange: LastSelected = Şube B (remember=true), sonra Şube B'yi soft-delete et
        using (var db = CreateDbContext())
        {
            await db.Users
                .Where(u => u.Id == _userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.LastSelectedBranchOfficeId, _officeBId)
                    .SetProperty(x => x.RememberLastBranchOffice, true));
            // Şube B soft-delete
            await db.BranchOffices
                .Where(b => b.Id == _officeBId)
                .ExecuteUpdateAsync(b => b
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletedAt, DateTimeOffset.UtcNow));
        }

        var (accessor, _) = CreateAccessor();

        var result = await accessor.ResolveAndStoreAsync(_userId);

        result.Should().Be(_officeAId, "Şube B silindi, DefaultBranchOffice=Şube A'ya fallback");
    }

    [Fact]
    public async Task Resolve_falls_back_to_HQ_when_user_has_no_default_and_no_valid_last()
    {
        // Arrange: kullanıcının DefaultBranchOfficeId'sini temizle, LastSelected yok
        using (var db = CreateDbContext())
        {
            await db.Users
                .Where(u => u.Id == _userId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.DefaultBranchOfficeId, (int?)null)
                    .SetProperty(x => x.RememberLastBranchOffice, false));
            // Junction'ı da temizle ki erişim sadece HQ olsun
            await db.UserBranchOffices
                .Where(j => j.UserId == _userId)
                .ExecuteDeleteAsync();
        }

        var (accessor, _) = CreateAccessor();

        var result = await accessor.ResolveAndStoreAsync(_userId);

        result.Should().Be(_hqId, "ne default ne remember → HQ'ya fallback");
    }

    [Fact]
    public async Task Switch_rejects_when_user_has_no_access_to_target()
    {
        // Arrange: kullanıcının Şube B'ye erişimi yok (junction'da değil, default da değil)
        var (accessor, _) = CreateAccessor();

        var result = await accessor.SwitchAsync(_userId, _officeBId, remember: false);

        result.Success.Should().BeFalse("kullanıcı Şube B'ye atanmamış");
        result.Message.Should().Contain("erişim");
    }

    [Fact]
    public async Task Switch_allows_HQ_for_any_user()
    {
        var (accessor, httpCtx) = CreateAccessor();

        var result = await accessor.SwitchAsync(_userId, _hqId, remember: false);

        result.Success.Should().BeTrue("HQ herkese açık");
        httpCtx.HttpContext!.Session.GetInt32("ActiveBranchOfficeId").Should().Be(_hqId);
    }

    [Fact]
    public async Task Switch_persists_to_user_when_remember_true()
    {
        var (accessor, _) = CreateAccessor();

        var result = await accessor.SwitchAsync(_userId, _officeAId, remember: true);

        result.Success.Should().BeTrue();

        using var db = CreateDbContext();
        var user = await db.Users.FirstAsync(u => u.Id == _userId);
        user.LastSelectedBranchOfficeId.Should().Be(_officeAId);
        user.RememberLastBranchOffice.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAndResetIfStale_returns_true_when_session_branch_still_valid()
    {
        var (accessor, httpCtx) = CreateAccessor();

        // Önce resolve et — Şube A session'a yazılır
        await accessor.ResolveAndStoreAsync(_userId);

        // Sonra validate — hâlâ geçerli olmalı
        var isValid = await accessor.ValidateAndResetIfStaleAsync(_userId);

        isValid.Should().BeTrue();
        httpCtx.HttpContext!.Session.GetInt32("ActiveBranchOfficeId").Should().Be(_officeAId);
    }

    [Fact]
    public async Task ValidateAndResetIfStale_resets_to_HQ_when_session_branch_soft_deleted()
    {
        var (accessor, httpCtx) = CreateAccessor();
        await accessor.ResolveAndStoreAsync(_userId); // Session = Şube A

        // Başka bir süreç Şube A'yı sildi (simülasyon)
        using (var db = CreateDbContext())
        {
            await db.BranchOffices
                .Where(b => b.Id == _officeAId)
                .ExecuteUpdateAsync(b => b
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletedAt, DateTimeOffset.UtcNow));
        }

        var isValid = await accessor.ValidateAndResetIfStaleAsync(_userId);

        isValid.Should().BeFalse("Şube A silindi");
        httpCtx.HttpContext!.Session.GetInt32("ActiveBranchOfficeId").Should().Be(_hqId,
            "stale session HQ'ya reset edilmeli");
    }
}

// ────────────────────────────────────────────────────────────────────────
// Test helpers
// ────────────────────────────────────────────────────────────────────────

/// <summary>
/// IHttpContextAccessor mock'u — her testte yeni bir DefaultHttpContext + FakeSession.
/// </summary>
internal sealed class FakeHttpContextAccessor : IHttpContextAccessor
{
    private HttpContext? _ctx;

    public FakeHttpContextAccessor()
    {
        var defaultCtx = new DefaultHttpContext();
        defaultCtx.Features.Set<ISessionFeature>(new FakeSessionFeature());
        _ctx = defaultCtx;
    }

    public HttpContext? HttpContext
    {
        get => _ctx;
        set => _ctx = value;
    }
}

internal sealed class FakeSessionFeature : ISessionFeature
{
    public ISession Session { get; set; } = new FakeSession();
}

/// <summary>
/// In-memory ISession — testlerde ActiveBranchOfficeAccessor session erişimini simüle eder.
/// </summary>
internal sealed class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value)
    {
        if (_store.TryGetValue(key, out var v))
        {
            value = v;
            return true;
        }
        value = Array.Empty<byte>();
        return false;
    }
}
