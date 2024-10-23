namespace EtsyStats.Models.Options;

public class ConfigurationOptions
{
    public const string SectionName = "Configuration";

    public int WaitDelayInSeconds { get; set; }
    public int MinDelayInMilliseconds { get; set; }
    public int MaxDelayInMilliseconds { get; set; }
}