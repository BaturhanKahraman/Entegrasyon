using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontEmailService
{
    Task<IResult> SendEmailVerificationAsync(string toEmail, string customerName, string verificationToken, string storeName, string domain);
    Task<IResult> SendPasswordResetAsync(string toEmail, string customerName, string resetToken, string storeName, string domain);
    Task<IResult> SendWelcomeAsync(string toEmail, string customerName, string storeName);
    Task<IResult> SendOrderConfirmationAsync(string toEmail, string customerName, string orderNumber, decimal total, string storeName, string domain);
    Task<IResult> SendAsync(string toEmail, string subject, string htmlBody);
}
