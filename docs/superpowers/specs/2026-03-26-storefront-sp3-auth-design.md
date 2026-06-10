# SP-3: Uyelik & Auth — Tasarim Dokumani

## Ozet

Storefront musteri uyelik sistemi: kayit, giris, email dogrulama, Şifre sifirlama, Google OAuth, hesap paneli. Cookie-based authentication. Mevcut Customer entity'sine bagli StorefrontCustomerAuth entity.

## Kapsam

**Dahil:** StorefrontCustomerAuth entity + migration, kayit (RetailCustomer + auth), giris (email+Şifre cookie-based), Şifre sifirlama (token), email dogrulama (token + endpoint), hesap paneli (/hesabim profil/Şifre), brute force korumasi, KVKK onay, Google OAuth 2.0

**Haric:** Gercek email gonderimi (SP-5), Sipariş gecmisi icerigi (SP-4 sonrasi), 2FA (Faz 2), adres CRUD detayi (SP-4)

---

## 1. Entity: StorefrontCustomerAuth : BaseEntity

Id (int), TenantId (int), CustomerId (int FK->Customer), Email (string unique per tenant), PasswordHash (byte[]), PasswordSalt (byte[]), EmailConfirmed (bool), EmailConfirmationToken (string?), EmailConfirmationTokenExpiresAt (DateTimeOffset?), PasswordResetToken (string?), PasswordResetTokenExpiresAt (DateTimeOffset?), LastLoginAt (DateTimeOffset?), LoginFailedCount (int), LockedUntil (DateTimeOffset?), MarketingConsent (bool), MarketingConsentDate (DateTimeOffset?), KvkkConsentDate (DateTimeOffset), ExternalLoginProvider (string?), ExternalLoginId (string?)

## 2. DTOs

StorefrontRegisterDto: TenantId, Name, Surname, Email, Phone, Password, KvkkConsent (bool), MarketingConsent (bool)
StorefrontLoginDto: Email, Password, RememberMe (bool)
StorefrontForgotPasswordDto: Email
StorefrontResetPasswordDto: Token, NewPassword, ConfirmPassword
StorefrontChangePasswordDto: CurrentPassword, NewPassword, ConfirmPassword
StorefrontProfileDto: Name, Surname, Email, Phone

## 3. Business: IStorefrontAuthManager

RegisterAsync(StorefrontRegisterDto) -> IDataResult<StorefrontCustomerAuth>
LoginAsync(int tenantId, string email, string password) -> IDataResult<StorefrontCustomerAuth>
ConfirmEmailAsync(int tenantId, string token) -> IResult
RequestPasswordResetAsync(int tenantId, string email) -> IDataResult<string> (token)
ResetPasswordAsync(int tenantId, string token, string newPassword) -> IResult
ChangePasswordAsync(int authId, string currentPassword, string newPassword) -> IResult
GetAuthByCustomerIdAsync(int tenantId, int customerId) -> IDataResult<StorefrontCustomerAuth>
UpdateProfileAsync(int authId, StorefrontProfileDto dto) -> IResult
ExternalLoginAsync(int tenantId, string provider, string externalId, string email, string name, string surname) -> IDataResult<StorefrontCustomerAuth>

Uses: IDbContextFactory, ICustomerManager (for RetailCustomer creation), HashingHelper (mevcut), RandomNumberGenerator (token)

## 4. Cookie Auth (Program.cs)

AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(LoginPath=/giris, LogoutPath=/cikis, ExpireTimeSpan=30 days, SlidingExpiration, Cookie.Name=Storefront.Auth, HttpOnly, SameSite=Lax).AddGoogle(ClientId/Secret from config, CallbackPath=/signin-google)

UseAuthentication() + UseAuthorization() after TenantResolutionMiddleware

## 5. Controllers

AuthController: Login(GET/POST), Register(GET/POST), Logout, ForgotPassword(GET/POST), ResetPassword(GET/POST), ConfirmEmail, GoogleLogin, GoogleCallback
AccountController [Authorize]: Index, Profile(GET/POST), ChangePassword(GET/POST), Orders (stub), Addresses (stub)

## 6. Routes

/giris, /kayit, /cikis, /Şifremi-unuttum, /Şifre-sifirla, /email-dogrula, /signin-google, /hesabim, /hesabim/profil, /hesabim/Şifre-Değiştir, /hesabim/Siparişlerim, /hesabim/adreslerim

## 7. Views

Auth/: Login, Register, ForgotPassword, ResetPassword, ConfirmEmail
Account/: Index, Profile, ChangePassword, Orders (stub), Addresses (stub)
Shared/_AccountLayout.cshtml: sidebar + content

## 8. Security

HashingHelper (HMACSHA512), brute force (5 fail -> 15 min lock), token (RandomNumberGenerator 32 bytes Base64Url), email dogrulama 24h, Şifre sifirlama 1h, CSRF (ValidateAntiForgeryToken), cookie HttpOnly+SameSite

## 9. Tests

Unit: StorefrontAuthManagerTests (register, login success/fail/locked, email confirm, password reset, change password)
