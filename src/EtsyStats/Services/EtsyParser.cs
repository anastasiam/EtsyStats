using EtsyStats.Constants.Etsy;
using EtsyStats.Extensions;
using EtsyStats.Helpers;
using EtsyStats.Models;
using EtsyStats.Models.Enums;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Selenium.Extensions;
using Selenium.WebDriver.UndetectedChromeDriver;

namespace EtsyStats.Services;

public class EtsyParser
{
    // TODO use appsettings.jsom
    private const string UserDataDirectory = "C:\\Users\\Administrator\\AppData\\Local\\Google\\Chrome\\User Data";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";

    private const string Href = "href";
    private const string Src = "src";

    private readonly SlDriver _chromeDriver;
    private readonly WebScrapingService _webScrapingService;
    private readonly Config _config;

    public EtsyParser(string chromeLocation, Config config)
    {
        _config = config;
        _chromeDriver = GetUndetectableChromeDriver(chromeLocation);
        _webScrapingService = new WebScrapingService(_chromeDriver);
    }

    public async Task<List<ListingStats>> GetListingsStats(DateRange dateRange)
    {
        List<ListingStats> listings = new();

        for (var page = 1;; page++)
        {
            var url = EtsyUrl.GetListingsUrl(page);

            var html = await _webScrapingService.NavigateAndLoadHtmlFromUrl(url, ListingsPageXPaths.ListingsListElement, ListingsPageXPaths.EmptyStateDiv);
            if (html == null)
            {
                await ProgramHelper.OriginalOut.WriteLineAsync("Finished parsing listings.");
                return listings;
            }

            var pageListings = await GetListingsFromPage(html, dateRange);
            listings.AddRange(pageListings);
        }

        return listings;
    }

    public async Task<List<SearchQueryAnalytics>> GetSearchAnalytics(DateRange dateRange)
    {
        var url = EtsyUrl.GetSearchAnalyticsUrl(dateRange);
        await _webScrapingService.NavigateAndLoadHtmlFromUrl(url, SearchAnalyticsPageXPaths.SearchQueryFirstTableCellFullXPath);

        List<SearchQueryAnalytics> searchAnalytics = new();
        var page = 1;
        do
        {
            await ProgramHelper.OriginalOut.WriteLineAsync($"Loading data from page {page}...");

            try
            {
                var searchAnalyticsRows = _chromeDriver.FindElements(By.XPath(SearchAnalyticsPageXPaths.TableRow));
                searchAnalytics.AddRange(searchAnalyticsRows.Select(searchAnalyticsRow => new SearchQueryAnalytics
                {
                    SearchQuery = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.SearchQueryTableCell)).Text.Trim(),
                    Impressions = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ImpressionsTableCell)).Text.Trim(),
                    Position = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.PositionTableCell)).Text.Trim(),
                    Visits = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.VisitsTableCell)).Text,
                    ConversionRate = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ConversionRateTableCell)).Text.Trim().ExtractNumber(),
                    Revenue = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.RevenueTableCell)).Text.ExtractNumber(),
                    Listings = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ListingsTableCell)).Text.Trim()
                }));
            }
            catch (Exception e)
            {
                await Log.Error($"Exception on parsing analytics on page {page}.\r\n{e.GetBaseException()}");
                await ProgramHelper.OriginalOut.WriteLineAsync($"An error occured on parsing analytics on page {page}.");
                await File.AppendAllTextAsync("logs/last_search_analytics.html", _chromeDriver.PageSource);
                throw;
            }

            page++;
        } while (await _webScrapingService.NextPage(string.Empty, SearchAnalyticsPageXPaths.SearchQueryFirstTableCellFullXPath));

        return searchAnalytics;
    }

    private async Task<List<ListingStats>> GetListingsFromPage(string html, DateRange dateRange)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        var listingsElements = htmlDocument.DocumentNode.SelectNodes(ListingsPageXPaths.ListingsListElement);

        List<ListingStats> listings = new();
        foreach (var listingElement in listingsElements)
        {
            var link = listingElement.SelectSingleNode(ListingsPageXPaths.ListingLink).Attributes[Href].Value;
            var id = EtsyUrl.GetListingIdFromLink(link);
            var url = EtsyUrl.GetListingStatsUrl(id, dateRange);

            var listingStatsHtml = await _webScrapingService.NavigateAndLoadHtmlFromUrl(url, ListingStatsPageXPaths.Title);

            var listing = new ListingStats
            {
                Link = EtsyUrl.GetListingUrl(id)
            };

            try
            {
                await LoadListingStatsFromPage(listingStatsHtml!, id, listing);
            }
            catch (Exception e)
            {
                await ProgramHelper.OriginalOut.WriteLineAsync($"An error occured while parsing listing {id}.");
                await Log.Error($"Exception on parsing stats for listing {id}.\r\n{e.GetBaseException()}");
            }

            listings.Add(listing);
        }

        return listings;
    }

    private async Task LoadListingStatsFromPage(string html, string id, ListingStats listing)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var imgSrc = htmlDoc.DocumentNode.SelectSingleNode(ListingStatsPageXPaths.TitlePhotoUrl(id)).Attributes[Src].Value;

        listing.TitlePhotoUrl = imgSrc;
        listing.Title = htmlDoc.DocumentNode.SelectSingleNode(ListingStatsPageXPaths.Title).InnerText.Trim();

        ParseStatsPageGeneralData(listing, htmlDoc);

        ParseStatsPageTrafficSources(listing, htmlDoc);

        await ParseStatsPageSearchTerms(listing, htmlDoc);
    }

    private void ParseStatsPageGeneralData(ListingStats listing, HtmlDocument htmlDoc)
    {
        var statsDataDropdown = htmlDoc.DocumentNode.SelectSingleNode(ListingStatsPageXPaths.GeneralDataDropdown);

        listing.Visits = decimal.Parse(statsDataDropdown.SelectSingleNode(ListingStatsPageXPaths.VisitsDropdownElement).InnerText.Trim());
        listing.TotalViews = decimal.Parse(statsDataDropdown.SelectSingleNode(ListingStatsPageXPaths.TotalViewsDropdownElement).InnerText.Trim());
        listing.Orders = decimal.Parse(statsDataDropdown.SelectSingleNode(ListingStatsPageXPaths.OrdersDropdownElement).InnerText.Trim());
        listing.Revenue = decimal.Parse(statsDataDropdown.SelectSingleNode(ListingStatsPageXPaths.RevenueDropdownElement).InnerText.ExtractNumber());
    }

    private void ParseStatsPageTrafficSources(ListingStats listing, HtmlDocument htmlDoc)
    {
        var trafficSourcesList = htmlDoc.DocumentNode.SelectSingleNode(ListingStatsPageXPaths.TrafficSourcesList);

        listing.DirectAndOtherTraffic = trafficSourcesList.SelectSingleNode(ListingStatsPageXPaths.DirectAndOtherTraffic).InnerText.ExtractNumber();
        listing.EtsyAppAndOtherEtsyPages = trafficSourcesList.SelectSingleNode(ListingStatsPageXPaths.EtsyAppAndOtherEtsyPages).InnerText.ExtractNumber();
        listing.EtsyAds = trafficSourcesList.SelectSingleNode(ListingStatsPageXPaths.EtsyAds).InnerText.ExtractNumber();
        listing.EtsyMarketingAndSeo = trafficSourcesList.SelectSingleNode(ListingStatsPageXPaths.EtsyMarketingAndSeo).InnerText.ExtractNumber();
        listing.SocialMedia = trafficSourcesList.SelectSingleNode(ListingStatsPageXPaths.SocialMedia).InnerText.ExtractNumber();
        listing.EtsySearch = trafficSourcesList.SelectSingleNode(ListingStatsPageXPaths.EtsySearch).InnerText.ExtractNumber();
    }

    private async Task ParseStatsPageSearchTerms(ListingStats listing, HtmlDocument htmlDoc)
    {
        var searchTermsTable = htmlDoc.DocumentNode.SelectSingleNode(ListingStatsPageXPaths.SearchTermsTable);
        var searchTermRows = searchTermsTable.SelectNodes(ListingStatsPageXPaths.SearchTermRow);

        var searchTerms = new List<(string name, string totalVisits)>();
        if (searchTermRows is not null)
        {
            do
            {
                foreach (var searchTermRow in searchTermRows)
                {
                    var searchTerm = searchTermRow.SelectSingleNode(ListingStatsPageXPaths.SearchTermCell).InnerText.Trim();

                    // Sometimes it's three columns (Etsy, Google, Total visits) in search terms, sometimes one (Visits)
                    var totalVisitsXPath =
                        // If 'Total visits' column exists
                        searchTermsTable.SelectSingleNode(ListingStatsPageXPaths.TotalVisitsColumn) is not null
                            ? ListingStatsPageXPaths.TotalVisitsCell // path to 'Total visits cell
                            : ListingStatsPageXPaths.VisitsCell; // path to 'Visits' cell

                    var totalVisits = searchTermRow.SelectSingleNode(totalVisitsXPath).InnerText.Trim();
                    searchTerms.Add((searchTerm, totalVisits));
                }
            } while (await _webScrapingService.NextPage(ListingStatsPageXPaths.SearchTermsNextButton, ListingStatsPageXPaths.SearchTermAnyCellFullXPath));

            listing.SearchTerms = string.Join(", ", searchTerms.Select(searchTerm => $"{searchTerm.name}: {searchTerm.totalVisits}"));
        }
    }

    private SlDriver GetUndetectableChromeDriver(string chromeLocation)
    {
        var options = ChromeOptions(chromeLocation);

        return UndetectedChromeDriver.Instance(_config.ChromeProfile, options);
    }

    private ChromeOptions ChromeOptions(string chromeLocation)
    {
        var options = new ChromeOptions
        {
            BinaryLocation = chromeLocation
        };

        options.AddArguments("headless");
        options.AddArguments($"--user-agent={UserAgent}");

        // TODO use login, password
        options.AddArguments($"--profile-directory={_config.ChromeProfile}");
        options.AddArguments($"--user-data-dir={UserDataDirectory}");

        return options;
    }
}