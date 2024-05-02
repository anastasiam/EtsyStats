using EtsyStats.Attributes;

namespace EtsyStats.Models;

public class SearchQueryAnalytics
{
    [SheetColumn("Search query", Order = 1)]
    public string? SearchQuery { get; set; }

    [SheetColumn("Impressions", Order = 2)]
    public string? Impressions { get; set; }

    [SheetColumn("Position", Order = 3)] public string? Position { get; set; }

    [SheetColumn("Visits", Order = 4)] public string? Visits { get; set; }

    [SheetColumn("ConversionRate", Order = 5)]
    public string? ConversionRate { get; set; }

    [SheetColumn("Revenue", Order = 6)] public string? Revenue { get; set; }

    [SheetColumn("Listings", Order = 7)] public string? Listings { get; set; }
}