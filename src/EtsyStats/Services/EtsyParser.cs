using EtsyStats.Constants.Etsy;
using EtsyStats.Extensions;
using EtsyStats.Helpers;
using EtsyStats.Models;
using EtsyStats.Models.Enums;
using EtsyStats.Models.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Selenium.Extensions;
using Selenium.WebDriver.UndetectedChromeDriver;

namespace EtsyStats.Services;

public class EtsyParser
{
    private const string UserDataDirectory = "Google/Chrome/User Data";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";
    private const string CategorySeparator = " / ";

    private const string Href = "href";
    private const string Src = "src";
    private const string InnerText = "innerText";
    
    private readonly Config _config;
    private readonly GoogleChromeOptions _googleChromeOptions;

    public EtsyParser(Config config, GoogleChromeOptions googleChromeOptions)
    {
        _config = config;
        _googleChromeOptions = googleChromeOptions;
    }

    public async Task<List<ListingStats>> GetListingsStats(DateRange dateRange)
    {
        using var chromeDriver = GetUndetectableChromeDriver(_googleChromeOptions.ChromeLocation);
        using var webScrapingService = new WebScrapingService(chromeDriver);
        
        List<ListingStats> listings = new();
        for (var page = 1;; page++)
        {
            var url = EtsyUrl.Listings(page);

            var pageLoadedSuccessfully = await webScrapingService.NavigateAndLoadHtmlFromUrl(url, ListingsPageXPaths.ListingLink, ListingsPageXPaths.EmptyStateDiv);
            if (!pageLoadedSuccessfully)
            {
                await Log.InfoAndConsole("Finished parsing listings.");
                return listings;
            }

            var pageListings = await GetListingsFromPage(chromeDriver, webScrapingService, dateRange);
            listings.AddRange(pageListings);
        }
    }

    public async Task<List<SearchQueryAnalytics>> GetSearchAnalytics(DateRange dateRange)
    {
        using var chromeDriver = GetUndetectableChromeDriver(_googleChromeOptions.ChromeLocation);
        using var webScrapingService = new WebScrapingService(chromeDriver);
        
        var url = EtsyUrl.SearchAnalytics(dateRange);
        await webScrapingService.NavigateAndLoadHtmlFromUrl(url, SearchAnalyticsPageXPaths.SearchQueryFirstTableCellFullXPath);

        List<SearchQueryAnalytics> searchAnalytics = new();
        var page = 1;
        do
        {
            await ProgramHelper.OriginalOut.WriteLineAsync($"Loading data from page {page}...");

            try
            {
                var searchAnalyticsRows = chromeDriver.FindElements(By.XPath(SearchAnalyticsPageXPaths.TableRow));
                searchAnalytics.AddRange(searchAnalyticsRows.Select(searchAnalyticsRow => new SearchQueryAnalytics
                {
                    SearchQuery = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.SearchQueryTableCell)).GetAttribute(InnerText).Trim(),
                    Impressions = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ImpressionsTableCell)).GetAttribute(InnerText).Trim(),
                    Position = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.PositionTableCell)).GetAttribute(InnerText).Trim(),
                    Visits = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.VisitsTableCell)).GetAttribute(InnerText),
                    ConversionRate = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ConversionRateTableCell)).GetAttribute(InnerText).Trim().ExtractNumber(),
                    Revenue = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.RevenueTableCell)).GetAttribute(InnerText).ExtractNumber(),
                    Listings = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ListingsTableCell)).GetAttribute(InnerText).Trim()
                }));
            }
            catch (Exception e)
            {
                await Log.Error($"Exception on parsing analytics on page {page}.\r\n{e.GetBaseException()}");
                await ProgramHelper.OriginalOut.WriteLineAsync($"An error occured on parsing analytics on page {page}.");
                await File.AppendAllTextAsync($"logs/last_search_analytics_page_{page}.html", chromeDriver.PageSource);
                throw;
            }

            page++;
        } while (await webScrapingService.NextPage(string.Empty, SearchAnalyticsPageXPaths.SearchQueryFirstTableCellFullXPath));

        return searchAnalytics;
    }

    private async Task<List<ListingStats>> GetListingsFromPage(SlDriver chromeDriver, WebScrapingService webScrapingService, DateRange dateRange)
    {
        // TODO use API to get listings?
        var listingsIds = webScrapingService.HandleStaleElements(() =>
        {
            var listingLinksElements = chromeDriver.FindElements(By.XPath(ListingsPageXPaths.ListingLink));
            return listingLinksElements.Select(e => EtsyUrl.GetListingIdFromLink(e.GetAttribute(Href))).ToList();
        });
        
        List<ListingStats> listings = new();
        for (var i = 0; i < listingsIds.Count; i++)
        {
            var id = listingsIds[i];
            await ProgramHelper.OriginalOut.WriteLineAsync($"\nProcessing listing {i + 1} out of  {listingsIds.Count}...\n");

            var listing = new ListingStats
            {
                Link = EtsyUrl.Listing(id)
            };

            try
            {
                await LoadListingFromStatsPage(chromeDriver, webScrapingService, id, dateRange, listing);
                await LoadListingFromEditPage(chromeDriver, webScrapingService, id, listing);
            }
            catch (Exception e)
            {
                await ProgramHelper.OriginalOut.WriteLineAsync($"\nAn error occured while parsing listing {id}.");
                await Log.Error($"Exception on parsing stats for listing {id}.\r\n{e.GetBaseException()}");
                await File.AppendAllTextAsync($"logs/last_stats_id_{id}.html", chromeDriver.PageSource);
                throw;
            }

            listings.Add(listing);
        }

        return listings;
    }

    private async Task LoadListingFromStatsPage(SlDriver chromeDriver, WebScrapingService webScrapingService, string id, DateRange dateRange, ListingStats listing)
    {
        var url = EtsyUrl.ListingStats(id, dateRange);
        await webScrapingService.NavigateAndLoadHtmlFromUrl(url, ListingStatsPageXPaths.Title);

        var imgSrc = chromeDriver.FindElement(By.XPath(ListingStatsPageXPaths.TitlePhotoUrl(id))).GetAttribute(Src);
        listing.TitlePhotoUrl = imgSrc;
        listing.Title = chromeDriver.FindElement(By.XPath(ListingStatsPageXPaths.Title)).GetAttribute(InnerText).Trim();

        ParseStatsPageGeneralData(chromeDriver, listing);

        ParseStatsPageTrafficSources(chromeDriver, listing);

        await ParseStatsPageSearchTerms(chromeDriver, webScrapingService, listing);
    }

    private async Task LoadListingFromEditPage(SlDriver chromeDriver, WebScrapingService webScrapingService, string id, ListingStats listing)
    {
        var url = EtsyUrl.ListingEdit(id);
        await webScrapingService.NavigateAndLoadHtmlFromUrl(url, ListingEditPageXPaths.ShopSection);

        var tags = chromeDriver.FindElements(By.XPath(ListingEditPageXPaths.Tag));
        var categories = chromeDriver.FindElements(By.XPath(ListingEditPageXPaths.Category));
        var shopSectionSelector = new SelectElement(chromeDriver.FindElement(By.XPath(ListingEditPageXPaths.ShopSection)));

        if (tags is not null)
            listing.Tags = tags.Select(t => t.GetAttribute(InnerText).Trim()).ToList();

        if (categories is not null)
            listing.Category = string.Join(CategorySeparator, categories.Select(c => c.GetAttribute(InnerText).Trim()));

        listing.ShopSection = shopSectionSelector.SelectedOption.GetAttribute(InnerText);
    }

    private void ParseStatsPageGeneralData(SlDriver chromeDriver, ListingStats listing)
    {
        var statsDataDropdown = chromeDriver.FindElement(By.XPath(ListingStatsPageXPaths.GeneralDataDropdown));

        listing.Visits = decimal.Parse(statsDataDropdown.FindElement(By.XPath(ListingStatsPageXPaths.VisitsDropdownElement)).GetAttribute(InnerText).Trim());
        listing.TotalViews = decimal.Parse(statsDataDropdown.FindElement(By.XPath(ListingStatsPageXPaths.TotalViewsDropdownElement)).GetAttribute(InnerText).Trim());
        listing.Orders = decimal.Parse(statsDataDropdown.FindElement(By.XPath(ListingStatsPageXPaths.OrdersDropdownElement)).GetAttribute(InnerText).Trim());
        listing.Revenue = decimal.Parse(statsDataDropdown.FindElement(By.XPath(ListingStatsPageXPaths.RevenueDropdownElement)).GetAttribute(InnerText).ExtractNumber());
    }

    private void ParseStatsPageTrafficSources(SlDriver chromeDriver, ListingStats listing)
    {
        var trafficSourcesList = chromeDriver.FindElement(By.XPath(ListingStatsPageXPaths.TrafficSourcesList));

        if (trafficSourcesList is null) return;

        listing.DirectAndOtherTraffic = trafficSourcesList.FindElement(By.XPath(ListingStatsPageXPaths.DirectAndOtherTraffic)).GetAttribute(InnerText).ExtractNumber();
        listing.EtsyAppAndOtherEtsyPages = trafficSourcesList.FindElement(By.XPath(ListingStatsPageXPaths.EtsyAppAndOtherEtsyPages)).GetAttribute(InnerText).ExtractNumber();
        listing.EtsyAds = trafficSourcesList.FindElement(By.XPath(ListingStatsPageXPaths.EtsyAds)).GetAttribute(InnerText).ExtractNumber();
        listing.EtsyMarketingAndSeo = trafficSourcesList.FindElement(By.XPath(ListingStatsPageXPaths.EtsyMarketingAndSeo)).GetAttribute(InnerText).ExtractNumber();
        listing.SocialMedia = trafficSourcesList.FindElement(By.XPath(ListingStatsPageXPaths.SocialMedia)).GetAttribute(InnerText).ExtractNumber();
        listing.EtsySearch = trafficSourcesList.FindElement(By.XPath(ListingStatsPageXPaths.EtsySearch)).GetAttribute(InnerText).ExtractNumber();
    }

    private async Task ParseStatsPageSearchTerms(SlDriver chromeDriver, WebScrapingService webScrapingService, ListingStats listing)
    {
        var searchTermsTable = chromeDriver.FindElement(By.XPath(ListingStatsPageXPaths.SearchTermsTable));
        var searchTermRows = searchTermsTable.FindElements(By.XPath(ListingStatsPageXPaths.SearchTermRow));

        listing.SearchTerms = new List<SearchTerm>();
        if (searchTermRows is not null)
        {
            do
            {
                foreach (var searchTermRow in searchTermRows)
                {
                    var searchTerm = searchTermRow.FindElement(By.XPath(ListingStatsPageXPaths.SearchTermCell)).GetAttribute(InnerText).Trim();
                    var totalVisits = searchTermRow.FindElement(By.XPath(ListingStatsPageXPaths.TotalVisitsCell)).GetAttribute(InnerText).Trim();

                    listing.SearchTerms.Add(new SearchTerm { Name = searchTerm, TotalVisits = totalVisits });
                }
            } while (await webScrapingService.NextPage(ListingStatsPageXPaths.SearchTermsNextButton, ListingStatsPageXPaths.SearchTermAnyCellFullXPath));
        }
    }

    private SlDriver GetUndetectableChromeDriver(string chromeLocation)
    {
        var options = ChromeOptions(chromeLocation);

        return UndetectedChromeDriver.Instance(_config.ChromeProfile, options);
    }

    private ChromeOptions ChromeOptions(string chromeLocation)
    {
        var userDataDirectory = $"{Environment.SpecialFolder.LocalApplicationData}/{UserDataDirectory}";

        var options = new ChromeOptions
        {
            BinaryLocation = chromeLocation
        };

        options.AddArguments("headless");
        options.AddArguments($"--user-agent={UserAgent}");

        options.AddArguments($"--profile-directory={_config.ChromeProfile}");
        options.AddArguments($"--user-data-dir={userDataDirectory}");

        return options;
    }
}