namespace EtsyStats.Models.Options;

public class ApplicationOptions
{
    public const string SectionName = "Application";

    public string? UserDataDirectory { get; set; }
    public string? ConfigFileName { get; set; }
}