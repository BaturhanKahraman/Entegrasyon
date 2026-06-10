using Entegrasyon.MVC.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace Entegrasyon.Test.Business;

/// <summary>
/// LastSeenAt yazımının throttle mantığı: her istekte DB-write yapma; kullanıcı başına
/// en fazla minInterval'da bir izin ver. Multi-tenant: anahtar userId, izole state.
/// </summary>
public class LastSeenThrottleTests
{
    [Fact]
    public void ShouldWrite_FirstTimeForUser_ReturnsTrue()
    {
        var throttle = new LastSeenThrottle(TimeSpan.FromMinutes(1));
        var now = DateTimeOffset.UtcNow;

        throttle.ShouldWrite(Guid.NewGuid(), now).Should().BeTrue();
    }

    [Fact]
    public void ShouldWrite_WithinInterval_ReturnsFalse()
    {
        var throttle = new LastSeenThrottle(TimeSpan.FromMinutes(1));
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        throttle.ShouldWrite(userId, now).Should().BeTrue();
        throttle.ShouldWrite(userId, now.AddSeconds(30)).Should().BeFalse();
    }

    [Fact]
    public void ShouldWrite_AfterInterval_ReturnsTrue()
    {
        var throttle = new LastSeenThrottle(TimeSpan.FromMinutes(1));
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        throttle.ShouldWrite(userId, now).Should().BeTrue();
        throttle.ShouldWrite(userId, now.AddMinutes(2)).Should().BeTrue();
    }

    [Fact]
    public void ShouldWrite_DifferentUsers_AreIsolated()
    {
        var throttle = new LastSeenThrottle(TimeSpan.FromMinutes(1));
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        throttle.ShouldWrite(userA, now).Should().BeTrue();
        // Farklı kullanıcı kendi interval'ına tabi — A'nın yazımı B'yi etkilemez.
        throttle.ShouldWrite(userB, now.AddSeconds(5)).Should().BeTrue();
    }
}
