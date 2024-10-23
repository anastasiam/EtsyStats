using EtsyStats.Attributes;

namespace EtsyStats.Models;

public class SearchTerm
{
    [SheetColumn("Search Term", Order = 10)]
    public string? Name { get; set; }

    [SheetColumn("Total Visits", Order = 20)]
    public int? TotalVisits { get; set; }
}