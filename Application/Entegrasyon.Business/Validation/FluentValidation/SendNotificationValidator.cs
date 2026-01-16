using Entegrasyon.Entity.Notifications;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public sealed class SendNotificationValidator:AbstractValidator<Notification>
{
    public SendNotificationValidator()
    {
        RuleFor(n => n.Content).NotEmpty().MinimumLength(5);
    }
}
