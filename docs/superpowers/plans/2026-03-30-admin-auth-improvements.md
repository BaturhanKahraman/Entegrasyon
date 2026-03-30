# Admin Auth Improvements — Implementation Plan

**Date:** 2026-03-30
**Spec:** `docs/superpowers/specs/2026-03-30-admin-auth-improvements-design.md`
**Branch:** develop

---

## Task 1: Entity Changes + Migration [x]

1. Add fields to `ApplicationUser`:
   - `FailedLoginCount`, `LockoutEnd`
   - `PasswordHashVersion`, `BcryptPasswordHash`
   - `TwoFactorSecret`, `TwoFactorRecoveryCodes`
   - `PasswordResetToken`, `PasswordResetTokenExpiresAt`
2. Add `BCrypt.Net-Next` v4.* to `Entegrasyon.Business.csproj`
3. `dotnet ef migrations add AdminAuthImprovements ...`
4. `dotnet ef database update ...`
5. `dotnet build` — verify clean
6. Commit

---

## Task 2: bcrypt Hash Migration (TDD) [x]

1. Add `CreateBcryptHash` + `VerifyBcryptHash` to `HashingHelper`
2. Update `AuthService.LoginAsync` — version-aware verify + auto-migrate
3. Update `AuthService.ChangeOwnPassword` + `CreatePassword` — use bcrypt for new hashes
4. Write tests in `AuthServiceTests`:
   - `CreateBcryptHash_Returns_ValidBcryptString`
   - `VerifyBcryptHash_WithCorrectPassword_ReturnsTrue`
   - `VerifyBcryptHash_WithWrongPassword_ReturnsFalse`
   - `LoginAsync_LegacyUser_AutoMigratesToBcrypt`
   - `LoginAsync_BcryptUser_VerifiesCorrectly`
5. `dotnet test` — all pass
6. Commit

---

## Task 3: Account Lockout (TDD) [x]

1. Update `AuthService.LoginAsync`:
   - Check `LockoutEnd` before verifying password
   - Increment `FailedLoginCount` on failure, lock on 5th
   - Reset on success
2. Write tests:
   - `LoginAsync_SuccessfulLogin_ResetFailedCount`
   - `LoginAsync_4Failures_NoLockout`
   - `LoginAsync_5Failures_AccountLocked`
   - `LoginAsync_LockedAccount_ReturnsLockedError`
   - `LoginAsync_ExpiredLockout_LoginSucceeds`
3. `dotnet test` — all pass
4. Commit

---

## Task 4: 2FA TOTP (TDD) [x]

1. Add `TwoFactorSetupDto` to `Entegrasyon.Entity/Dtos/Auth/`
2. Add `RequiresTwoFactorResult` to `Entegrasyon.Entity/Results/`
3. Add methods to `IAuthService`
4. Implement in `AuthService`:
   - `SetupTwoFactorAsync` — generate OtpNet secret, return DTO
   - `EnableTwoFactorAsync` — verify TOTP, set `IsTwoFactorAuthActive = true`
   - `DisableTwoFactorAsync` — verify TOTP, clear secret + flag
   - `GenerateRecoveryCodesAsync` — 10 codes, SHA256-hash stored in JSON
   - `VerifyTwoFactorAsync` — TOTP or recovery code
   - `LoginAsync` — after success, return `RequiresTwoFactorResult` if 2FA active
5. Write tests:
   - `SetupTwoFactorAsync_ReturnsSetupDto`
   - `EnableTwoFactorAsync_WithValidCode_Enables2FA`
   - `DisableTwoFactorAsync_WithValidCode_Disables2FA`
   - `GenerateRecoveryCodesAsync_Returns10Codes`
   - `LoginAsync_With2FAActive_ReturnsRequiresTwoFactor`
   - `VerifyTwoFactorAsync_WithValidRecoveryCode_Succeeds`
6. `dotnet test` — all pass
7. Commit

---

## Task 5: Password Reset Token (TDD) [x]

1. Add methods to `IAuthService`
2. Implement in `AuthService`:
   - `RequestPasswordResetAsync` — find by email, generate token (base64 random), expiry 1hr
   - `ResetPasswordAsync` — find by token, check expiry, bcrypt new pw, clear token + lockout
3. Add messages to `Messages.cs`
4. Write tests:
   - `RequestPasswordResetAsync_ValidEmail_GeneratesToken`
   - `RequestPasswordResetAsync_UnknownEmail_ReturnsSuccess` (no info leak)
   - `ResetPasswordAsync_ValidToken_Succeeds`
   - `ResetPasswordAsync_ExpiredToken_Fails`
   - `ResetPasswordAsync_InvalidToken_Fails`
5. `dotnet test` — all pass
6. Commit

---

## Task 6: Final Verification [x]

1. `dotnet build Entegrasyon.sln` — clean
2. `dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj` — all pass
3. Commit if any fixes needed
