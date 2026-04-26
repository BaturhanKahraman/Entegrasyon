using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete.Auth;

/// <summary>
/// ICurrentUserContext'in boş (null-object) implementasyonu.
/// Gerçek HTTP bağlamı olmayan ortamlarda (test, arka plan servisi) fallback olarak kullanılır.
/// MVC katmanı kendi CurrentUserContext'ini Program.cs üzerinden override eder.
/// </summary>
public sealed class NullCurrentUserContext : ICurrentUserContext
{
    public Guid? UserId => null;
}
