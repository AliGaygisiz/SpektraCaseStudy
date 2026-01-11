namespace SpektraCaseStudy.Infrastructure.Configuration;

public class WorkerSettings
{
    public int FlushIntervalSeconds { get; set; } = 2;
    public int CleanupIntervalMinutes { get; set; } = 1;
    public int ColdEventThresholdMinutes { get; set; } = 10;
    public int DuplicateEventThresholdSeconds { get; set; } = 600;
}
