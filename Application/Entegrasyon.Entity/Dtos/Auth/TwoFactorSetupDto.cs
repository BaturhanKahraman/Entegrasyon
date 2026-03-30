namespace Entegrasyon.Entity.Dtos.Auth;

public sealed record TwoFactorSetupDto(string Secret, string QrCodeUri, string ManualEntryKey);
