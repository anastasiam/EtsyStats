using EtsyStats.Models;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace EtsyStats.Services;

public class GoogleSheetService
{
    private const string GoogleCredentialsFileName = "google-credentials.json";
    private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

    // TODO Make property attribute with column name and order
    private static readonly List<object> ColumnNames = new()
    {
        "Title Photo",
        "Link",
        "Visits",
        "Total Views",
        "Orders",
        "Revenue",
        "Conversion Rate",
        "Click Rate",
        "Direct & other traffic",
        "Etsy app & other Etsy pages",
        "Etsy Ads",
        "Etsy marketing & SEO",
        "Social media",
        "Etsy search",
        "Title",
        "Search Terms"
    };

    private static readonly List<object> SearchAnalyticsColumnNames = new()
    {
        "Search query",
        "Impressions",
        "Position",
        "Visits",
        "Conversion rate",
        "Revenue",
        "Listings"
    };

    public async Task WriteListingsToSheet(string sheetId, string shop, List<ListingStats> listings)
    {
        var tabName = $"{shop} - Stats";
        var service = GetSheetsService();

        var valuesResource = service.Spreadsheets.Values;

        var clear = valuesResource.Clear(new ClearValuesRequest(), sheetId, $"{tabName}!1:1000");
        await clear.ExecuteAsync();

        // Headers
        var valueRange = new ValueRange { Values = new List<IList<object>> { ColumnNames }, MajorDimension = "ROWS" };

        // Values
        foreach (var listing in listings)
        {
            valueRange.Values.Add(new List<object>
            {
                $@"=IMAGE(""{listing.TitlePhotoUrl}"")",
                listing.Link,
                listing.Visits,
                listing.TotalViews,
                listing.Orders,
                listing.Revenue,
                listing.ConversionRate,
                listing.ClickRate,
                listing.DirectAndOtherTraffic,
                listing.EtsyAppAndOtherEtsyPages,
                listing.EtsyAds,
                listing.EtsyMarketingAndSeo,
                listing.SocialMedia,
                listing.EtsySearch,
                listing.Title,
                listing.SearchTerms
            });
        }

        var update = valuesResource.Update(valueRange, sheetId, $"{tabName}!A1:P{listings.Count + 2}");

        update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        var response = await update.ExecuteAsync();
        ProgramHelper.OriginalOut.WriteLine($@"\nUpdated rows:{response.UpdatedRows}");
    }

    public async Task WriteSearchAnalyticsToSheet(string sheetId, string shop, List<SearchQueryAnalytics> searchQueriesAnalytics)
    {
        var tabName = $"{shop} - Analytics";
        var service = GetSheetsService();

        try
        {
            await WriteSearchAnalyticsToSheet(service, sheetId, tabName, searchQueriesAnalytics);
        }
        catch (GoogleApiException)
        {
            var request = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new() { AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = tabName, Index = 0 } } },
                }
            };
            var batchUpdate = service.Spreadsheets.BatchUpdate(request, sheetId);
            await batchUpdate.ExecuteAsync();

            await WriteSearchAnalyticsToSheet(service, sheetId, tabName, searchQueriesAnalytics);
        }
    }

    private async Task WriteSearchAnalyticsToSheet(SheetsService service, string sheetId, string tabName, List<SearchQueryAnalytics> searchQueriesAnalytics)
    {
        var valuesResource = service.Spreadsheets.Values;
        var clear = valuesResource.Clear(new ClearValuesRequest(), sheetId, $"{tabName}!1:1000");
        await clear.ExecuteAsync();

        // Headers
        var valueRange = new ValueRange { Values = new List<IList<object>> { SearchAnalyticsColumnNames }, MajorDimension = "ROWS" };

        // Values
        foreach (var listing in searchQueriesAnalytics)
        {
            valueRange.Values.Add(new List<object>
            {
                listing.SearchQuery,
                listing.Impressions,
                listing.Position,
                listing.Visits,
                listing.ConversionRate,
                listing.Revenue,
                listing.Listings
            });
        }

        var update = valuesResource.Update(valueRange, sheetId, $"{tabName}!A1:G{searchQueriesAnalytics.Count + 2}");

        update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

        var response = await update.ExecuteAsync();
        ProgramHelper.OriginalOut.WriteLine($@"Updated rows: {response.UpdatedRows}");
    }

    private static SheetsService GetSheetsService()
    {
        using var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read);
        var serviceInitializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
        };

        return new SheetsService(serviceInitializer);
    }
}