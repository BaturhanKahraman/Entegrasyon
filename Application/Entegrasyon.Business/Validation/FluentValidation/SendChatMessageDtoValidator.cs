using Entegrasyon.Entity.Dtos.Chat;
using FluentValidation;

namespace Entegrasyon.Business.Validation.FluentValidation;

public class SendChatMessageDtoValidator : AbstractValidator<SendChatMessageDto>
{
    public SendChatMessageDtoValidator()
    {
        RuleFor(x => x.ConversationId).GreaterThan(0);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}
