using EtsyStats.Models.Enums;

namespace EtsyStats.Helpers;

public abstract class EtsyUrl
{
    private const string UrlSeparator = "/";

    public static string Listings(int page)
    {
        return $"https://www.etsy.com/your/shops/me/tools/listings/page:{page},stats:true";
    }

    public static string Listing(string id)
    {
        return $"https://www.etsy.com/listing/{id}";
    }

    public static string ListingStats(string id, DateRange dateRange)
    {
        return $"https://www.etsy.com/your/shops/me/stats/listings/{id}?{DateRangeQueryParameter.GetQueryStats(dateRange)}";
    }

    public static string ListingEdit(string id)
    {
        return $"https://www.etsy.com/your/shops/me/listing-editor/edit/{id}";
    }

    public static string GetListingIdFromLink(string url)
    {
        return url.Split(UrlSeparator).ToList().Last();
    }
}