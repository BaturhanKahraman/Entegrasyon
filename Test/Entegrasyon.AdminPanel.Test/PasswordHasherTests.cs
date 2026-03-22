using FluentAssertions;
using Entegrasyon.AdminPanel.Infrastructure.Auth;

namespace Entegrasyon.AdminPanel.Test;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ShouldReturnNonEmptyString()
    {
        var hash = PasswordHasher.Hash("password123");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().Contain(".");
    }

    [Fact]
    public void Hash_ShouldReturnDifferentHashesForSamePassword()
    {
        var hash1 = PasswordHasher.Hash("password123");
        var hash2 = PasswordHasher.Hash("password123");

        hash1.Should().NotBe(hash2, "her hash benzersiz salt kullanmalı");
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatches()
    {
        var password = "güçlü-şifre-123";
        var hash = PasswordHasher.Hash(password);

        PasswordHasher.Verify(password, hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        var hash = PasswordHasher.Hash("doğru-şifre");

        PasswordHasher.Verify("yanlış-şifre", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenHashFormatIsInvalid()
    {
        PasswordHasher.Verify("password", "invalid-hash-format").Should().BeFalse();
    }
}
