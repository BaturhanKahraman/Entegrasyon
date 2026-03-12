using Entegrasyon.Business.Notifications;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public sealed class SendNotificationValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.Header).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MinimumLength(5).MaximumLength(1000);
        RuleFor(x => x.UserIds).NotEmpty();
        RuleFor(x => x.ActionUrl).MaximumLength(500).When(x => x.ActionUrl != null);
    }
}
