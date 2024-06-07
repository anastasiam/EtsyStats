using EtsyStats.Attributes;
using EtsyStats.Extensions;
using EtsyStats.Helpers;

namespace EtsyStats.Models;

public class ListingStats
{
    private const string ListSeparator = ", ";
    private const string WordsSeparator = " ";
    
    public string? TitlePhotoUrl { get; set; }

    [SheetColumn("Title Photo", Order = 1)]
    public string? TitlePhoto => GoogleSheetsFormula.GetImageFromUrl(TitlePhotoUrl);

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

    public List<SearchTerm>? SearchTerms { get; set; } 

    [SheetColumn("Search terms", Order = 16)]
    public string? SearchTermsFormatted => SearchTerms is not null ? string.Join(ListSeparator, SearchTerms.Select(searchTerm => $"{searchTerm.Name}: {searchTerm.TotalVisits}")) : null;

    public List<string>? Tags { get; set; }

    [SheetColumn("Tags", Order = 17)] public string? TagsFormatted => Tags is not null ? string.Join(ListSeparator, Tags) : null;

    [SheetColumn("Working Tags", Order = 18)]
    public string? WorkingTagsFormatted
    {
        get
        {
            if (Tags is null || SearchTerms is null) return null;

            return string.Join(ListSeparator, Tags.Where(t => SearchTerms.Any(st => t.Equals(st.Name, StringComparison.OrdinalIgnoreCase))));
        }
    }

    [SheetColumn("Non-working Tags", Order = 19)]
    public string? NonWorkingTags
    {
        get
        {
            if (Tags is null || SearchTerms is null) return null;

            return string.Join(ListSeparator, Tags.Where(t => !SearchTerms.Any(st => t.Equals(st.Name, StringComparison.OrdinalIgnoreCase))).ToList());
        }
    }

    private IEnumerable<string?>? SearchTermsWords => SearchTerms?.SelectMany(st => st.Name?.Split(WordsSeparator, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
    private IEnumerable<string>? TitleWords => Title?.ReplaceSpecialCharacters(WordsSeparator).Split(WordsSeparator, StringSplitOptions.RemoveEmptyEntries);
    
    [SheetColumn("Working Title Words", Order = 20)]
    public string? WorkingTitleWords
    {
        get
        {
            if (TitleWords is null || SearchTermsWords is null) return null;

            return string.Join(ListSeparator, TitleWords.Where(t => SearchTermsWords.Any(st => t.Equals(st, StringComparison.OrdinalIgnoreCase))));
        }
    }

    [SheetColumn("Non-working Title Words", Order = 21)]
    public string? NonWorkingTitleWords
    {
        get
        {
            if (TitleWords is null || SearchTermsWords is null) return null;

            return string.Join(ListSeparator, TitleWords.Where(t => !SearchTermsWords.Any(st => t.Equals(st, StringComparison.OrdinalIgnoreCase))));
        }
    }

    [SheetColumn("Tagging ideas (Tags)", Order = 22)]
    public string? TaggingIdeasTags
    {
        get
        {
            if (SearchTerms is null || Tags is null) return null;

            return string.Join(ListSeparator, SearchTerms.Where(st => !Tags.Any(t => t.Equals(st.Name, StringComparison.OrdinalIgnoreCase))).Select(st => st.Name));
        }
    }

    [SheetColumn("Tagging ideas (Title)", Order = 23)]
    public string? TaggingIdeasTitle
    {
        get
        {
            if (TitleWords is null || SearchTermsWords is null) return null;

            return string.Join(ListSeparator, SearchTermsWords.Where(st => !TitleWords.Any(t => t.Equals(st, StringComparison.OrdinalIgnoreCase))));
        }
    }

    [SheetColumn("Category", Order = 24)] public string? Category { get; set; }

    [SheetColumn("Shop section", Order = 25)]
    public string? ShopSection { get; set; }
}