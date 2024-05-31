namespace EtsyStats.Models.Options;

public class ConfigurationOptions
{
    public const string SectionName = "Configuration";

    public int WaitForPageToLoadDelayInSeconds { get; set; }
    public int MinDelayInMilliseconds { get; set; }
    public int MaxDelayInMilliseconds { get; set; }
}