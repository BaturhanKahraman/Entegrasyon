using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.MVC.Features.Storefront.ViewModels;

public class MessagesVm
{
    public List<StorefrontContactMessage> ContactMessages { get; set; } = [];
    public List<StorefrontProductQuestion> UnansweredQuestions { get; set; } = [];
    public string ActiveTab { get; set; } = "contact";
}
