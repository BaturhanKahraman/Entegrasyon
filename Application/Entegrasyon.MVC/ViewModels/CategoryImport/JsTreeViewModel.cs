namespace Entegrasyon.MVC.ViewModels.CategoryImport;
public sealed class JsTreeViewModel
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string Icon { get; set; }
    public State State { get; set; }
    public JsTreeViewModel[] Children { get; set; }
}

public sealed class State
{
    public bool Opened { get; set; }
    public bool Disabled { get; set; }
    public bool Selected { get; set; }
}
