namespace Entegrasyon.Business.Channels.Events.System;

public sealed class SyncErrorThresholdExceededEvent : BaseEvent
{
    public string ServiceName { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public int WindowMinutes { get; set; }

    public SyncErrorThresholdExceededEvent() { }
    public SyncErrorThresholdExceededEvent(string serviceName, int errorCount, int windowMinutes)
    {
        ServiceName = serviceName;
        ErrorCount = errorCount;
        WindowMinutes = windowMinutes;
    }
}
