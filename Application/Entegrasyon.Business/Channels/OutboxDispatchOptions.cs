namespace Entegrasyon.Business.Channels;

public sealed class OutboxDispatchOptions
{
    public const string SectionName = "Notifications:Outbox";

    public TimeSpan SafetyPollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan StuckProcessingCutoff { get; set; } = TimeSpan.FromMinutes(10);
    public int BatchSize { get; set; } = 50;
    public int MaxRetryCount { get; set; } = 5;
    public int MaxConcurrency { get; set; } = 10;
}
