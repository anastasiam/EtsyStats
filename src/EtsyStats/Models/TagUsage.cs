using EtsyStats.Attributes;

namespace EtsyStats.Models;

public class TagUsage
{
    [SheetColumn("Tag", Order = 10)] 
    public string? Name { get; set; }

    [SheetColumn("Usage Count", Order = 20)]
    public int? UsageCount { get; set; }
}