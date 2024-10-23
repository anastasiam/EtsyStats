namespace EtsyStats.Models;

public class Statistic
{
    public List<ListingStatistic>? ListingStatistics { get; set; }

    public List<SearchTerm>? SearchTermStatistics { get; set; }

    public List<TagUsage>? TagStatistics { get; set; }
}