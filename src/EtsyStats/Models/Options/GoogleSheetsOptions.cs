namespace EtsyStats.Models.Options;

public class GoogleSheetsOptions
{
    public const string SectionName = "GoogleSheets";

    public string? SpreadsheetId { get; set; }

    public string? GoogleCredentialsFileName { get; set; }
}