using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Hepsiburada iade/değişim talepleri servisi.
/// </summary>
public sealed class HepsiburadaClaimService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHepsiburadaApiClient apiClient,
    ILogger<HepsiburadaClaimService> logger) : IHepsiburadaClaimService
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;

    private async Task<string> GetMerchantIdAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var marketplace = await dbContext.MarketPlaces
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == HbMarketPlaceId)
            ?? throw new InvalidOperationException("Hepsiburada marketplace kaydı bulunamadı.");
        return marketplace.SellerId ?? throw new InvalidOperationException("Hepsiburada merchantId tanımlı değil.");
    }

    public async Task<IDataResult<List<HepsiburadaClaimDto>>> GetClaimsAsync(string? status = null)
    {
        try
        {
            var merchantId = await GetMerchantIdAsync();
            var url = string.IsNullOrEmpty(status)
                ? $"/claims/merchantId/{merchantId}"
                : $"/claims/merchantId/{merchantId}/status/{status}";

            var response = await apiClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new ErrorDataResult<List<HepsiburadaClaimDto>>(null, $"Claim API hatası: {response.StatusCode}");

            var claims = await response.Content.ReadFromJsonAsync<List<HepsiburadaClaimDto>>();
            return new SuccessDataResult<List<HepsiburadaClaimDto>>(claims ?? []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB claims list failed");
            return new ErrorDataResult<List<HepsiburadaClaimDto>>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> AcceptClaimAsync(string claimNumber)
    {
        try
        {
            var response = await apiClient.PostAsync($"/claims/number/{claimNumber}/accept", new { });

            if (!response.IsSuccessStatusCode)
                return new ErrorResult($"Talep kabul hatası: {response.StatusCode}");

            logger.LogInformation("HB claim accepted: {ClaimNumber}", claimNumber);
            return new SuccessResult("İade/değişim talebi kabul edildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB claim accept failed: {ClaimNumber}", claimNumber);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> RejectClaimAsync(string claimNumber, string reason)
    {
        try
        {
            var response = await apiClient.PostAsync($"/claims/number/{claimNumber}/reject", new { reason });

            if (!response.IsSuccessStatusCode)
                return new ErrorResult($"Talep red hatası: {response.StatusCode}");

            logger.LogInformation("HB claim rejected: {ClaimNumber}", claimNumber);
            return new SuccessResult("İade/değişim talebi reddedildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB claim reject failed: {ClaimNumber}", claimNumber);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}
