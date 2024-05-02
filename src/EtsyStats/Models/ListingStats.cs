using EtsyStats.Attributes;

namespace EtsyStats.Models;

public class ListingStats
{
    public string? TitlePhotoUrl { get; set; }

    [SheetColumn("Title Photo", Order = 1)]
    public string TitlePhoto => $@"=IMAGE(""{TitlePhotoUrl}"")";

    [SheetColumn("Link", Order = 2)] public string? Link { get; set; }

    [SheetColumn("Visits", Order = 3)]
    public decimal Visits { get; set; }

    [SheetColumn("Total Views", Order = 4)]
    public decimal TotalViews { get; set; }

    [SheetColumn("Orders", Order = 5)]
    public decimal Orders { get; set; }

    [SheetColumn("Revenue", Order = 6)]
    public decimal Revenue { get; set; }

    [SheetColumn("Conversion Rate", Order = 7)]
    public decimal ConversionRate => Visits == 0 ? 0 : Math.Round(Orders / Visits * 100, 2);

    [SheetColumn("Click Rate", Order = 8)]
    public decimal ClickRate => TotalViews == 0 ? 0 : Math.Round(Visits / TotalViews * 100, 2);

    [SheetColumn("Direct & other traffic", Order = 9)]
    public string? DirectAndOtherTraffic { get; set; }

    [SheetColumn("Etsy app & other Etsy pages", Order = 10)]
    public string? EtsyAppAndOtherEtsyPages { get; set; }

    [SheetColumn("Etsy ads", Order = 11)] public string? EtsyAds { get; set; }

    [SheetColumn("Etsy marketing & SEO", Order = 12)]
    public string? EtsyMarketingAndSeo { get; set; }

    [SheetColumn("Social media", Order = 13)]
    public string? SocialMedia { get; set; }

    [SheetColumn("Etsy search", Order = 14)]
    public string? EtsySearch { get; set; }

    [SheetColumn("Title", Order = 15)] public string? Title { get; set; }

    [SheetColumn("Search terms", Order = 16)]
    public string? SearchTerms { get; set; }

    [SheetColumn("Tags", Order = 17)] public string? Tags { get; set; }

    [SheetColumn("Working Tags", Order = 18)]
    public string? WorkingTags { get; set; }

    [SheetColumn("Non-working Tags", Order = 19)]
    public string? NonWorkingTags { get; set; }

    [SheetColumn("Working Title Words", Order = 20)]
    public string? WorkingTitleWords { get; set; }

    [SheetColumn("Non-working Title Words", Order = 21)]
    public string? NonWorkingTitleWords { get; set; }

    [SheetColumn("Tagging ideas (Tags)", Order = 22)]
    public string? TaggingIdeasTags { get; set; }

    [SheetColumn("Tagging ideas (Title)", Order = 23)]
    public string? TaggingIdeasTitle { get; set; }
}