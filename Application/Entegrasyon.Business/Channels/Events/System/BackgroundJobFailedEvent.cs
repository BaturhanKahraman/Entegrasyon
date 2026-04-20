namespace Entegrasyon.Business.Channels.Events.System;

public sealed class BackgroundJobFailedEvent : BaseEvent
{
    public string JobName { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public int RetryCount { get; set; }

    public BackgroundJobFailedEvent() { }
    public BackgroundJobFailedEvent(string jobName, string error, int retryCount)
    {
        JobName = jobName;
        Error = error;
        RetryCount = retryCount;
    }
}
