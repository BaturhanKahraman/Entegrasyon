using Entegrasyon.Entity.Notifications;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public sealed class SendNotificationValidator:AbstractValidator<Notification>
{
    public SendNotificationValidator()
    {
        RuleFor(n => n.Content).NotEmpty().MinimumLength(5);
        RuleFor(n => n.Claims).NotEmpty().When(n => n.Users is null);
        RuleFor(n => n.Users).NotEmpty().When(n => n.Claims is null);
    }
}