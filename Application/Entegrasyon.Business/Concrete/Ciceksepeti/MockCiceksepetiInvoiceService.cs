using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Mock Çiçeksepeti fatura servisi — development ve test için.
/// Gerçek API çağrısı yapmaz; başarılı sonuç döndürür.
/// </summary>
public sealed class MockCiceksepetiInvoiceService(
    ILogger<MockCiceksepetiInvoiceService> logger) : ICiceksepetiInvoiceService
{
    public Task<IResult> SendInvoiceAsync(CiceksepetiInvoiceRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Çiçeksepeti send invoice: {Count} items", request.Items.Count);
        return Task.FromResult<IResult>(new SuccessResult("Fatura gönderildi (MOCK)."));
    }
}
