using EtsyStats.Attributes;
using EtsyStats.Extensions;
using EtsyStats.Helpers;

namespace EtsyStats.Models;

public class ListingStats
{
    private const string ListSeparator = ", ";
    private const string WordsSeparator = " ";
    
    public string? TitlePhotoUrl { get; set; }

    [SheetColumn("Title Photo", Order = 10)]
    public string? TitlePhoto => GoogleSheetsFormula.GetImageFromUrl(TitlePhotoUrl);

    [SheetColumn("Link", Order = 20)] public string? Link { get; set; }

    [SheetColumn("Listed date", Order = 21)]
    public string? ListedDate { get; set; }

    [SheetColumn("SKU", Order = 22)] public string? Sku { get; set; }

    [SheetColumn("Visits", Order = 30)]
    public decimal Visits { get; set; }

    [SheetColumn("Total Views", Order = 40)]
    public decimal TotalViews { get; set; }

    [SheetColumn("Orders", Order = 50)]
    public decimal Orders { get; set; }

    [SheetColumn("Revenue", Order = 60)]
    public decimal Revenue { get; set; }

    [SheetColumn("Conversion Rate", Order = 70)]
    public decimal ConversionRate => Visits == 0 ? 0 : Math.Round(Orders / Visits * 100, 2);

    [SheetColumn("Click Rate", Order = 80)]
    public decimal ClickRate => TotalViews == 0 ? 0 : Math.Round(Visits / TotalViews * 100, 2);

    [SheetColumn("Direct & other traffic", Order = 90)]
    public string? DirectAndOtherTraffic { get; set; }

    [SheetColumn("Etsy app & other Etsy pages", Order = 100)]
    public string? EtsyAppAndOtherEtsyPages { get; set; }

    [SheetColumn("Etsy ads", Order = 110)] public string? EtsyAds { get; set; }

    [SheetColumn("Etsy marketing & SEO", Order = 120)]
    public string? EtsyMarketingAndSeo { get; set; }

    [SheetColumn("Social media", Order = 130)]
    public string? SocialMedia { get; set; }

    [SheetColumn("Etsy search", Order = 140)]
    public string? EtsySearch { get; set; }

    [SheetColumn("Title", Order = 150)] public string? Title { get; set; }

    public List<SearchTerm>? SearchTerms { get; set; }

    [SheetColumn("Search terms", Order = 160)]
    public string? SearchTermsFormatted => SearchTerms is not null ? string.Join(ListSeparator, SearchTerms.Select(searchTerm => $"{searchTerm.Name}: {searchTerm.TotalVisits}")) : null;

    public List<string>? Tags { get; set; }

    [SheetColumn("Tags", Order = 170)] public string? TagsFormatted => Tags is not null ? string.Join(ListSeparator, Tags) : null;

    [SheetColumn("Working Tags", Order = 180)]
    public string? WorkingTagsFormatted
    {
        get
        {
            if (Tags is null || SearchTerms is null) return null;

            return string.Join(ListSeparator, Tags.Where(t => SearchTerms.Any(st => t.Equals(st.Name, StringComparison.OrdinalIgnoreCase))));
        }
    }

    [SheetColumn("Non-working Tags", Order = 190)]
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

    [SheetColumn("Working Title Words", Order = 200)]
    public string? WorkingTitleWords
    {
        get
        {
            if (TitleWords is null || SearchTermsWords is null) return null;

            return string.Join(ListSeparator, TitleWords.Where(t => SearchTermsWords.Any(st => t.Equals(st, StringComparison.OrdinalIgnoreCase))));
        }
    }

    [SheetColumn("Non-working Title Words", Order = 210)]
    public string? NonWorkingTitleWords
    {
        get
        {
            if (TitleWords is null || SearchTermsWords is null) return null;

            return string.Join(ListSeparator, TitleWords.Where(t => !SearchTermsWords.Any(st => t.Equals(st, StringComparison.OrdinalIgnoreCase))));
        }
    }

    [SheetColumn("Tagging ideas (Tags)", Order = 220)]
    public string? TaggingIdeasTags
    {
        get
        {
            if (SearchTerms is null || Tags is null) return null;

            return string.Join(ListSeparator, SearchTerms.Where(st => !Tags.Any(t => t.Equals(st.Name, StringComparison.OrdinalIgnoreCase))).Select(st => st.Name));
        }
    }

    [SheetColumn("Tagging ideas (Title)", Order = 230)]
    public string? TaggingIdeasTitle
    {
        get
        {
            if (TitleWords is null || SearchTermsWords is null) return null;

            return string.Join(ListSeparator, SearchTermsWords.Where(st => !TitleWords.Any(t => t.Equals(st, StringComparison.OrdinalIgnoreCase))));
        }
    }

    [SheetColumn("Category", Order = 240)] public string? Category { get; set; }

    [SheetColumn("Shop section", Order = 250)]
    public string? ShopSection { get; set; }

    [SheetColumn("Shipping profile", Order = 251)]
    public string? ShippingProfile { get; set; }
}