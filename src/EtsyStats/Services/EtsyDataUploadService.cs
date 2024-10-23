using EtsyStats.Models;
using Newtonsoft.Json;

namespace EtsyStats.Services;

public class EtsyDataUploadService
{
    private readonly GoogleSheetService _googleSheetService;
    private readonly UserConfiguration _userConfiguration;

    public EtsyDataUploadService(UserConfiguration userConfiguration,
        GoogleSheetService googleSheetService)
    {
        _userConfiguration = userConfiguration;
        _googleSheetService = googleSheetService;
    }

    /// <summary>
    /// Writes listing stats to "{UserConfiguration.ShopName} - Stats" tab of the sheet <paramref name="spreadsheetId"/> and creates tab if it doesn't exist.
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet identifier</param>
    /// <param name="listings">List of listings</param>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task WriteListingsStatisticsToGoogleSheet(string spreadsheetId, IList<ListingStatistic> listings)
    {
        var tabName = $"{_userConfiguration.ShopName} - Stats";

        if (listings is null)
        {
            throw new ArgumentNullException(nameof(listings));
        }

        if (!_googleSheetService.SheetExists(spreadsheetId, tabName))
            await _googleSheetService.CreateTab(spreadsheetId, tabName);

        await _googleSheetService.WriteDataToSheet(spreadsheetId, tabName, listings);
    }

    /// <summary>
    /// Writes statistic summary to "{UserConfiguration.ShopName} - Summary" tab of the sheet <paramref name="spreadsheetId"/> and creates tab if it doesn't exist.
    /// </summary>
    /// <param name="spreadsheetId">Google Sheet identifier</param>
    /// <param name="searchTermsSummary"></param>
    /// <param name="tagsSummary"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task WriteStatisticSummaryToGoogleSheet(string spreadsheetId, IList<SearchTerm> searchTermsSummary, IList<TagUsage> tagsSummary)
    {
        var tabName = $"{_userConfiguration.ShopName} - Summary";

        if (searchTermsSummary is null) throw new ArgumentNullException(nameof(searchTermsSummary));

        if (tagsSummary is null) throw new ArgumentNullException(nameof(tagsSummary));

        if (!_googleSheetService.SheetExists(spreadsheetId, tabName))
            await _googleSheetService.CreateTab(spreadsheetId, tabName);

        await Log.InfoAsync($"Writing SearchTermsSummary: {JsonConvert.SerializeObject(searchTermsSummary)}");
        var (updatedSearchTermsColumns, _) = await _googleSheetService.WriteDataToSheet(spreadsheetId, tabName, searchTermsSummary);

        // TODO: this to separate tab
        await Log.InfoAsync($"Appending TagsSummary: {JsonConvert.SerializeObject(tagsSummary)}");
        var firstColumnIndex = updatedSearchTermsColumns + 1; // +1 - for empty column and border column
        await _googleSheetService.AppendDataToSheet(spreadsheetId, tabName, tagsSummary, firstColumnIndex);
    }
}