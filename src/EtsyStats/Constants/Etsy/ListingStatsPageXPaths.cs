namespace EtsyStats.Constants.Etsy;

public static class ListingStatsPageXPaths
{
    private const string VisitsText = "Visits";
    private const string TotalViewsText = "Total Views";
    private const string OrdersText = "Orders";
    private const string RevenueText = "Revenue";
    private const string DirectAndOtherTrafficText = "Direct &amp; other traffic";
    private const string EtsyAppAndOtherEtsyPagesText = "Etsy app &amp; other Etsy pages";
    private const string EtsyAdsText = "Etsy Ads";
    private const string EtsyMarketingAndSeoText = "Etsy marketing &amp; SEO";
    private const string SocialMediaText = "Social media";
    private const string EtsySearchText = "Etsy search";
    private const string TotalVisits = "Total visits";
    private const string NextPage = "Next page";

    private const string GeneralDataDropdownElement = $"li/span/span[contains(text(), '{TextPlaceholder}')]/../div";
    private const string TrafficSource = $"//span[contains(text(), '{TextPlaceholder}')]/../../../../../div[2]/span[2]";

    private const string TextPlaceholder = "{text}";

    // Search Terms

    public const string SearchTermsTable = "//table[@id='horizontal-chart4']";
    public const string SearchTermRow = "//tr[not(contains(@class, 'column-header'))]";
    public const string SearchTermCell = "td/div/div[1]/div[1]/span[1]";
    public const string TotalVisitsColumn = $"//div[contains(text(), '{TotalVisits}')]";
    public const string TotalVisitsCell = "td/div/div[1]/div[2]/div[3]";
    public const string VisitsCell = "td/div/div[1]/div[2]/div[3]";
    public const string SearchTermAnyCellFullXPath = $"{SearchTermsTable}{SearchTermRow}/{SearchTermCell}";
    public const string SearchTermsNextButton = $"{SearchTermsTable}/../..//button[@title='{NextPage}']";

    // General Data

    public const string Title = @"//div[contains(@class, 'stats-page-list')]/div/div/div[2]/div[1]/div/div/div/div/div/div/div[2]/div[1]/div[1]/div/span";
    public const string GeneralDataDropdown = "//div[contains(@class, 'stats-page-list')]/div/div/div[4]//div[contains(@class, 'dropdown')]//ul";
    public static string VisitsDropdownElement => GeneralDataDropdownElement.Replace(TextPlaceholder, VisitsText);
    public static string TotalViewsDropdownElement => GeneralDataDropdownElement.Replace(TextPlaceholder, TotalViewsText);
    public static string OrdersDropdownElement => GeneralDataDropdownElement.Replace(TextPlaceholder, OrdersText);
    public static string RevenueDropdownElement => GeneralDataDropdownElement.Replace(TextPlaceholder, RevenueText);

    // Traffic Sources

    public const string TrafficSourcesList = "//div[contains(@class, 'stats-page-list')]//div/div/div[5]/div[1]/div[2]/div/div/div/div/div/ol";
    public static string DirectAndOtherTraffic => TrafficSource.Replace(TextPlaceholder, DirectAndOtherTrafficText);
    public static string EtsyAppAndOtherEtsyPages => TrafficSource.Replace(TextPlaceholder, EtsyAppAndOtherEtsyPagesText);
    public static string EtsyAds => TrafficSource.Replace(TextPlaceholder, EtsyAdsText);
    public static string EtsyMarketingAndSeo => TrafficSource.Replace(TextPlaceholder, EtsyMarketingAndSeoText);
    public static string SocialMedia => TrafficSource.Replace(TextPlaceholder, SocialMediaText);
    public static string EtsySearch => TrafficSource.Replace(TextPlaceholder, EtsySearchText);

    public static string TitlePhotoUrl(string id)
    {
        return $"//*[@id='mission-control-listing-stats']//a[contains(@href, '{id}')]//img[contains(@src, '170x135')]";
    }
}