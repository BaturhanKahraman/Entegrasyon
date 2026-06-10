using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Storefront;

public class IyzicoPaymentService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<IyzicoPaymentService> logger) : IPaymentGatewayService
{
    public async Task<IDataResult<PaymentInitResult>> InitiatePaymentAsync(PaymentRequest request)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var config = await dbContext.Set<StorefrontPaymentConfig>()
            .FirstOrDefaultAsync(c => c.IsActive && c.PaymentProvider == "Iyzico");

        if (config is null)
            return new ErrorDataResult<PaymentInitResult>(null!, "iyzico yapilandirilmamis.");

        var options = BuildOptions(config);

        var iyzicoRequest = new CreateCheckoutFormInitializeRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = request.OrderId.ToString(),
            Price = request.Amount.ToString("F2"),
            PaidPrice = request.Amount.ToString("F2"),
            Currency = Currency.TRY.ToString(),
            PaymentGroup = PaymentGroup.PRODUCT.ToString(),
            CallbackUrl = request.CallbackUrl,
            EnabledInstallments = new List<int> { 1, 2, 3, 6, 9, 12 },
            Buyer = new Buyer
            {
                Id = request.OrderId.ToString()[..8],
                Name = request.CustomerName,
                Surname = request.CustomerSurname,
                Email = request.CustomerEmail,
                GsmNumber = request.CustomerPhone,
                IdentityNumber = "11111111111", // dummy — checkout form does not require real TC
                RegistrationAddress = request.CustomerAddress,
                City = request.CustomerCity,
                Country = "Turkey",
                Ip = request.CustomerIp
            },
            ShippingAddress = new Address
            {
                ContactName = $"{request.CustomerName} {request.CustomerSurname}",
                City = request.CustomerCity,
                Country = "Turkey",
                Description = request.CustomerAddress
            },
            BillingAddress = new Address
            {
                ContactName = $"{request.CustomerName} {request.CustomerSurname}",
                City = request.CustomerCity,
                Country = "Turkey",
                Description = request.CustomerAddress
            },
            BasketItems = request.Items.Select(item => new BasketItem
            {
                Id = item.Id,
                Name = item.Name,
                Category1 = item.Category,
                ItemType = BasketItemType.PHYSICAL.ToString(),
                Price = item.Price.ToString("F2")
            }).ToList()
        };

        try
        {
            var result = await CheckoutFormInitialize.Create(iyzicoRequest, options);

            if (result.Status == "success")
            {
                return new SuccessDataResult<PaymentInitResult>(
                    new PaymentInitResult(result.PaymentPageUrl, result.Token));
            }

            logger.LogError("iyzico baslatma hatasi: {Error}", result.ErrorMessage);
            return new ErrorDataResult<PaymentInitResult>(null!, result.ErrorMessage ?? "Odeme baslatilamadi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "iyzico exception");
            return new ErrorDataResult<PaymentInitResult>(null!, "Odeme sistemi hatasi.");
        }
    }

    public async Task<IDataResult<PaymentCallbackResult>> HandleCallbackAsync(string token)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var config = await dbContext.Set<StorefrontPaymentConfig>()
            .FirstOrDefaultAsync(c => c.IsActive && c.PaymentProvider == "Iyzico");

        if (config is null)
            return new ErrorDataResult<PaymentCallbackResult>(null!, "iyzico yapilandirilmamis.");

        var options = BuildOptions(config);

        var request = new RetrieveCheckoutFormRequest { Token = token };

        try
        {
            var result = await CheckoutForm.Retrieve(request, options);

            if (result.PaymentStatus == "SUCCESS")
            {
                return new SuccessDataResult<PaymentCallbackResult>(
                    new PaymentCallbackResult(true, result.PaymentId, decimal.Parse(result.PaidPrice), null));
            }

            return new SuccessDataResult<PaymentCallbackResult>(
                new PaymentCallbackResult(false, null, null, result.ErrorMessage ?? "Odeme başarısız."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "iyzico callback exception");
            return new ErrorDataResult<PaymentCallbackResult>(null!, "Odeme dogrulama hatasi.");
        }
    }

    private static Options BuildOptions(StorefrontPaymentConfig config) => new()
    {
        ApiKey = config.ApiKey,
        SecretKey = config.SecretKey,
        BaseUrl = config.BaseUrl
                  ?? (config.IsLive ? "https://api.iyzipay.com" : "https://sandbox-api.iyzipay.com")
    };
}
