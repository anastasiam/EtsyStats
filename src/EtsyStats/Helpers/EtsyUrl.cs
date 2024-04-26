using EtsyStats.Models.Enums;

namespace EtsyStats.Helpers;

public static class EtsyUrl
{
    private const string ListingsUrl = $"https://www.etsy.com/your/shops/{ShopPlaceholder}/tools/listings/page:{PagePlaceholder},stats:true";
    private const string ListingStatsUrl = $"https://www.etsy.com/your/shops/{ShopPlaceholder}/stats/listings/{IdPlaceholder}?{DateRangePlaceholder}";
    private const string SearchAnalyticsUrl = $"https://www.etsy.com/your/shops/me/search-analytics?{DateRangePlaceholder}";
    private const string ListingLinkUrl = $"https://www.etsy.com/listing/{IdPlaceholder}";

    private const string DateRangePlaceholder = "{dateRange}";
    private const string ShopPlaceholder = "{shop}";
    private const string PagePlaceholder = "{page}";
    private const string IdPlaceholder = "{id}";
    
    private const string UrlSeparator = "/";

    public static string GetListingsUrl(string shop, int page, DateRange dateRange)
    {
        return ListingsUrl
            .Replace(ShopPlaceholder, shop)
            .Replace(PagePlaceholder, page.ToString())
            .Replace(DateRangePlaceholder, DateRangeQueryParameter.GetQuery(dateRange));
    }

    public static string GetListingStatsUrl(string shop, string id)
    {
        return ListingStatsUrl
            .Replace(ShopPlaceholder, shop)
            .Replace(IdPlaceholder, id);
    }

    public static string GetSearchAnalyticsUrl(string shop, DateRange dateRange)
    {
        return SearchAnalyticsUrl
            .Replace(ShopPlaceholder, shop)
            .Replace(DateRangePlaceholder, DateRangeQueryParameter.GetQuery(dateRange));
    }

    public static string GetListingUrl(string id)
    {
        return ListingLinkUrl.Replace(IdPlaceholder, id);
    }

    public static string GetListingIdFromLink(string url)
    {
        return url.Split(UrlSeparator).ToList().Last();
    }
}