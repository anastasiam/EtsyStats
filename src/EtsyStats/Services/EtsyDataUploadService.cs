using EtsyStats.Models;
using Google;

namespace EtsyStats.Services;

public class EtsyDataUploadService
{
    private const string StatsTab = $"{ShopPlaceholder} - Stats";
    private const string SearchAnalyticsTab = $"{ShopPlaceholder} - Analytics";

    private const string ShopPlaceholder = "{shop}";

    private readonly GoogleSheetService _googleSheetService;
    private readonly Config _config;

    public EtsyDataUploadService(Config config)
    {
        _config = config;
        _googleSheetService = new GoogleSheetService();
    }

    /// <summary>
    /// Writes data to "{shopName} - Stats" tab. Creates tab if doesn't exist.
    /// </summary>
    /// <param name="sheetId">Google Sheet identifier</param>
    /// <param name="listings">List of listings</param>
    public async Task WriteListingsStatsToGoogleSheet(string sheetId, List<ListingStats> listings)
    {
        var tabName = StatsTab.Replace(ShopPlaceholder, _config.ShopName);
        try
        {
            await _googleSheetService.WriteDataToSheet(sheetId, tabName, listings);
        }
        catch (GoogleApiException)
        {
            await _googleSheetService.CreateTab(sheetId, tabName);
            await _googleSheetService.WriteDataToSheet(sheetId, tabName, listings);
        }
    }

    /// <summary>
    /// Writes data to "{shopName} - Analytics" tab. Creates tab if doesn't exist.
    /// </summary>
    /// <param name="sheetId">Google Sheet identifier</param>
    /// <param name="searchAnalytics">List of analytics per search query</param>
    public async Task WriteSearchAnalyticsToGoogleSheet(string sheetId, List<SearchQueryAnalytics> searchAnalytics)
    {
        var tabName = SearchAnalyticsTab.Replace(ShopPlaceholder, _config.ShopName);

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