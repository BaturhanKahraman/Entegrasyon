# Admin Auth Improvements — Design Spec

**Date:** 2026-03-30
**Status:** Approved

## Overview

4 improvements to harden the admin authentication system. Patterns are copied directly from the existing Storefront auth implementation.

---

## Feature 1: Account Lockout

**Rule:** 5 failed login attempts → account locked for 15 minutes.

**Fields added to ApplicationUser:**
- `FailedLoginCount` (int) — incremented on each failed attempt, reset on success
- `LockoutEnd` (DateTimeOffset?) — set to `UtcNow + 15min` on 5th failure; null when unlocked

**Login flow changes:**
1. Check `LockoutEnd > UtcNow` → return locked error with remaining minutes
2. On failed verify → increment `FailedLoginCount`, lock if >= 5
3. On success → reset both fields to 0/null

**Error message (Turkish):** `"Hesabınız {N} dakika kilitli."`

---

## Feature 2: Password Hash Migration (HMACSHA512 → bcrypt)

**Motivation:** bcrypt is the industry standard for password hashing; HMACSHA512 is fast and not suitable for passwords.

**Strategy:** Backward-compatible migration — no forced re-hash, migrate lazily on successful login.

**Fields added to ApplicationUser:**
- `BcryptPasswordHash` (string?) — stores bcrypt hash
- `PasswordHashVersion` (int) — 0 = HMACSHA512 (legacy), 1 = bcrypt

**Login flow changes:**
1. If `PasswordHashVersion == 1`: verify with bcrypt
2. If `PasswordHashVersion == 0`: verify with HMACSHA512; on success auto-migrate: compute bcrypt hash, store in `BcryptPasswordHash`, set `PasswordHashVersion = 1`, null out legacy fields

**NuGet:** `BCrypt.Net-Next` v4.* added to Business project.

---

## Feature 3: Admin 2FA (TOTP)

**Pattern:** Copied from `StorefrontAuthManager` (OtpNet library already in csproj).

**Fields added to ApplicationUser** (already partially present — `IsTwoFactorAuthActive` exists):
- `TwoFactorSecret` (string?) — base32-encoded TOTP secret
- `TwoFactorRecoveryCodes` (string?) — JSON array of SHA256-hashed recovery codes

**New interface methods on IAuthService:**
```
SetupTwoFactorAsync(Guid userId) → TwoFactorSetupDto
EnableTwoFactorAsync(Guid userId, string code) → IResult
DisableTwoFactorAsync(Guid userId, string code) → IResult
GenerateRecoveryCodesAsync(Guid userId) → IDataResult<List<string>>
VerifyTwoFactorAsync(Guid userId, string code) → IResult
```

**Login flow changes:**
- After successful password verify + lockout clear: if `IsTwoFactorAuthActive == true` → return `RequiresTwoFactorResult` (special result type)
- Caller calls `VerifyTwoFactorAsync` to complete login with TOTP code or recovery code

**DTO:**
```csharp
public record TwoFactorSetupDto(string Secret, string QrCodeUri, string ManualEntryKey);
```

---

## Feature 4: Password Reset Token

**Pattern:** Copied from `StorefrontAuthManager.RequestPasswordResetAsync` / `ResetPasswordAsync`.

**Fields added to ApplicationUser:**
- `PasswordResetToken` (string?) — random base64 token
- `PasswordResetTokenExpiresAt` (DateTimeOffset?) — 1 hour from generation

**New interface methods:**
```
RequestPasswordResetAsync(string email) → IResult
ResetPasswordAsync(string token, string newPassword) → IResult
```

**Notes:**
- `RequestPasswordResetAsync`: silently succeeds even if email not found (security best practice — don't leak email existence)
- `ResetPasswordAsync`: validates token + expiry, then hashes new password with bcrypt (version 1)
- After reset: clear token, reset `FailedLoginCount`, clear `LockoutEnd`

---

## Non-Goals

- No UI changes in this iteration (settings page for 2FA planned separately)
- No email sending for password reset (token saved to DB; delivery is out of scope)
- No rate-limiting on reset requests
