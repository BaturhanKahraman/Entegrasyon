using System.Collections.Concurrent;

namespace Entegrasyon.MVC.Infrastructure.Security;

/// <summary>
/// LastSeenAt DB-write'ını kullanıcı başına throttle eder. Her istekte senkron DB-write
/// (perf ihlali) yapmamak için: bir kullanıcı için en fazla <c>minInterval</c>'da bir yazıma izin verir.
///
/// Multi-tenant: anahtar userId (Guid) → state kullanıcı bazında izole. Singleton servis;
/// in-memory state ConcurrentDictionary ile tutulur (tek field değil). Best-effort —
/// yeniden başlatmada cache sıfırlanır, sorun değil (en kötü ihtimalle 1 fazla yazım).
/// </summary>
public sealed class LastSeenThrottle(TimeSpan minInterval)
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastWrites = new();

    /// <summary>
    /// Verilen kullanıcı için <paramref name="now"/> anında DB'ye LastSeenAt yazılmalı mı?
    /// İlk kez görülüyorsa ya da son yazımdan beri en az minInterval geçtiyse true döner ve
    /// son-yazım damgasını günceller. Aksi halde false (yazma atlanır).
    /// </summary>
    public bool ShouldWrite(Guid userId, DateTimeOffset now)
    {
        var shouldWrite = false;
        _lastWrites.AddOrUpdate(
            userId,
            _ =>
            {
                shouldWrite = true;
                return now;
            },
            (_, last) =>
            {
                if (now - last >= minInterval)
                {
                    shouldWrite = true;
                    return now;
                }
                return last;
            });
        return shouldWrite;
    }
}
