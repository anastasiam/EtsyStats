using EtsyStats.Models.Enums;

namespace EtsyStats.Helpers;

public static class EtsyUrl
{
    private const string UrlSeparator = "/";

    public static string GetListingsUrl(int page)
    {
        return $"https://www.etsy.com/your/shops/me/tools/listings/page:{page},stats:true";
    }

    public static string GetListingStatsUrl(string id, DateRange dateRange)
    {
        return $"https://www.etsy.com/your/shops/me/stats/listings/{id}?{DateRangeQueryParameter.GetQueryStats(dateRange)}";
    }

    public static string GetSearchAnalyticsUrl(DateRange dateRange)
    {
        return $"https://www.etsy.com/your/shops/me/search-analytics?{DateRangeQueryParameter.GetQueryAnalytics(dateRange)}";
        
    }

    public static string GetListingUrl(string id)
    {
        return $"https://www.etsy.com/listing/{id}";
    }

    public static string GetListingIdFromLink(string url)
    {
        return url.Split(UrlSeparator).ToList().Last();
    }
}