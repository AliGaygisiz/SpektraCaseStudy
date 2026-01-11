namespace SpektraCaseStudy.Domain.Entities;

public class UserAggregate
{
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;

    public double SumValue { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }

    public long FirstTs { get; set; }
    public double FirstValue { get; set; }
    public long LastTs { get; set; }
    public double LastValue { get; set; }
}
