namespace Entegrasyon.MVC.Utility.ToastrNotification;

public class ToastrOption
{
    public bool CloseButton { get; set; } = false;
    public bool Debug { get; set; } = false;
    public bool NewestOnTop { get; set; } = false;
    public bool ProgressBar { get; set; } = false;
    public string PositionClass { get; set; }
    public bool PreventDuplicates { get; set; } = false;
    public object Onclick { get; set; }
    public string ShowDuration { get; set; } = "300";
    public string HideDuration { get; set; }="1000";
    public string TimeOut { get; set; }="5000";
    public string ExtendedTimeOut { get; set; } = "1000";
    public string ShowEasing { get; set; } = "swing";
    public string HideEasing { get; set; } = "linear";
    public string ShowMethod { get; set; } = "fadeIn";
    public string HideMethod { get; set; } = "fadeOut";
}