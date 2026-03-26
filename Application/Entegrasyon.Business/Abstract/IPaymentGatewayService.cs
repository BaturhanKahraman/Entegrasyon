using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPaymentGatewayService
{
    Task<IDataResult<PaymentInitResult>> InitiatePaymentAsync(PaymentRequest request);
    Task<IDataResult<PaymentCallbackResult>> HandleCallbackAsync(string token);
}

public record PaymentRequest(
    Guid OrderId,
    decimal Amount,
    string CustomerEmail,
    string CustomerName,
    string CustomerSurname,
    string CustomerPhone,
    string CustomerIp,
    string CustomerCity,
    string CustomerAddress,
    string CallbackUrl,
    List<PaymentItemDto> Items);

public record PaymentItemDto(
    string Name, string Category, decimal Price, string Id);

public record PaymentInitResult(
    string PaymentPageUrl,
    string Token);

public record PaymentCallbackResult(
    bool Success,
    string? TransactionId,
    decimal? PaidAmount,
    string? ErrorMessage);
