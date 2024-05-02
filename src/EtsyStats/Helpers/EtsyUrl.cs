using EtsyStats.Models.Enums;

namespace EtsyStats.Helpers;

public abstract class EtsyUrl
{
    private const string UrlSeparator = "/";

    public static string Listings(string shop, int page)
    {
        return $"https://www.etsy.com/your/shops/me/tools/listings/page:{page},stats:true";
    }

    public static string Listing(string id)
    {
        return $"https://www.etsy.com/listing/{id}";
    }

    public static string ListingStats(string shop, string id, DateRange dateRange)
    {
        return $"https://www.etsy.com/your/shops/{shop}/stats/listings/{id}?{dateRange}";
    }

    public static string ListingEdit(string shop, string id)
    {
        return $"https://www.etsy.com/your/shops/{shop}/listing-editor/edit/{id}";
    }

    public static string SearchAnalytics(string shop, DateRange dateRange)
    {
        return $"https://www.etsy.com/your/shops/{shop}/search-analytics?{dateRange}";
    }

    public static string GetListingIdFromLink(string url)
    {
        return url.Split(UrlSeparator).ToList().Last();
    }
}