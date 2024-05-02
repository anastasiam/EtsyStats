using EtsyStats.Models;
using Google;

namespace EtsyStats.Services;

public class EtsyDataUploadService
{
    private const string StatsTab = $"{ShopPlaceholder} - Stats";
    private const string SearchAnalyticsTab = $"{ShopPlaceholder} - Analytics";

    private const string ShopPlaceholder = "{shop}";

    private readonly GoogleSheetService _googleSheetService;

    public EtsyDataUploadService()
    {
        _googleSheetService = new GoogleSheetService();
    }

    public async Task WriteListingsStatsToGoogleSheet(string sheetId, string shopName, List<ListingStats> listings)
    {
        var tabName = StatsTab.Replace(ShopPlaceholder, shopName);
        await _googleSheetService.WriteDataToSheet(sheetId, tabName, listings);
    }

    /// <summary>
    /// Writes data to "{shopName} - Analytics" tab. Creates tab if doesn't exist.
    /// </summary>
    /// <param name="sheetId">Google Sheet identifier</param>
    /// <param name="shopName">Shop name</param>
    /// <param name="searchAnalytics">List of analytics per search query</param>
    public async Task WriteSearchAnalyticsToGoogleSheet(string sheetId, string shopName, List<SearchQueryAnalytics> searchAnalytics)
    {
        var tabName = SearchAnalyticsTab.Replace(ShopPlaceholder, shopName);

        try
        {
            await _googleSheetService.WriteDataToSheet(sheetId, tabName, searchAnalytics);
        }
        catch (GoogleApiException)
        {
            await _googleSheetService.CreateTab(sheetId, tabName);
            await _googleSheetService.WriteDataToSheet(sheetId, tabName, searchAnalytics);
        }
    }
}