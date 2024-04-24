using EtsyStats.Constants.Etsy;
using EtsyStats.Extensions;
using EtsyStats.Helpers;
using EtsyStats.Models;
using EtsyStats.Models.Enums;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace EtsyStats.Services;

public class EtsyParser
{
    private const string UrlSeparator = "/";
    private const string HrefAttribute = "href";

    private const string TextPlaceholder = "{text}";

    private readonly ChromeDriver _chromeDriver;
    private readonly WebsiteScrapingService _websiteScrapingService;

    public EtsyParser(string chromeLocation)
    {
        _chromeDriver = GetChromeDriver(chromeLocation);
        _websiteScrapingService = new WebsiteScrapingService(_chromeDriver);
    }

    public async Task<List<ListingStats>> GetListingsStats(string shop, DateRange dateRange)
    {
        List<ListingStats> listings = new();

        for (var page = 1;; page++)
        {
            var url = EtsyUrl.GetListingsUrl(shop, page, dateRange);

            var html = await _websiteScrapingService.NavigateAndLoadHtmlFromUrl(url, ListingsPageXPaths.ListingsListElement, ListingsPageXPaths.EmptyStateDiv);
            if (html == null)
            {
                Console.WriteLine("Finished parsing listings.");
                return listings;
            }

            var pageListings = await GetListingsFromPage(shop, html);

            listings.AddRange(pageListings);
        }
    }

    public async Task<List<SearchQueryAnalytics>> GetSearchAnalytics(string shop, DateRange dateRange)
    {
        // Test. Getting html from a file.
        // const string htmlStatsPageFileName = @"/Users/anastasiia/My Projects/EtsyStats.Test/pages/Analytics.html";
        // ProgramHelper.OriginalOut.WriteLine("\nReading file...");
        // var html = GetHtmlFromFile(htmlStatsPageFileName);
        // ProgramHelper.OriginalOut.WriteLine("\nParsing html...");
        // var htmlDocument = new HtmlDocument();
        // htmlDocument.LoadHtml(html);

        var url = EtsyUrl.GetSearchAnalyticsUrl(shop, dateRange);
        await _websiteScrapingService.NavigateAndLoadHtmlFromUrl(url, SearchAnalyticsPageXPaths.SearchQueryFirstTableCellFullXPath);

        List<SearchQueryAnalytics> searchAnalytics = new();

        var page = 1;
        do
        {
            await ProgramHelper.OriginalOut.WriteLineAsync($"Loading data from page {page}...");
            page++;

            var searchAnalyticsRows = _chromeDriver.FindElements(By.XPath(SearchAnalyticsPageXPaths.TableRow));
            searchAnalytics.AddRange(searchAnalyticsRows.Select(searchAnalyticsRow => new SearchQueryAnalytics
            {
                SearchQuery = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.SearchQueryTableCell)).Text.Trim(),
                Impressions = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ImpressionsTableCell)).Text.Trim(),
                Position = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.PositionTableCell)).Text.Trim(),
                Visits = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.VisitsTableCell)).Text.ExtractNumber(),
                ConversionRate = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ConversionRateTableCell)).Text.Trim(),
                Revenue = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.RevenueTableCell)).Text.ExtractNumber(),
                Listings = searchAnalyticsRow.FindElement(By.XPath(SearchAnalyticsPageXPaths.ListingsTableCell)).Text.Trim()
            }));
        } while (await _websiteScrapingService.NextPage(string.Empty, SearchAnalyticsPageXPaths.SearchQueryFirstTableCellFullXPath));

        return searchAnalytics;
    }

    private async Task<List<ListingStats>> GetListingsFromPage(string shop, string html)
    {
        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        var listingsElements = htmlDocument.DocumentNode.SelectNodes(ListingsPageXPaths.ListingsListElement);
        List<ListingStats> listings = new();

        foreach (var listingDiv in listingsElements)
        {
            var link = listingDiv.SelectSingleNode(ListingsPageXPaths.ListingLink).Attributes[HrefAttribute].Value;
            var id = link.Split(UrlSeparator).ToList().Last();

            var url = EtsyUrl.GetListingStatsUrl(shop, id);
            var listingStatsHtml = await _websiteScrapingService.NavigateAndLoadHtmlFromUrl(url, ListingStatsPageXPaths.Title);

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
#if DEBUG
                ProgramHelper.OriginalOut.WriteLine(e);
#endif

                await File.AppendAllTextAsync("logs/logs.txt", $"\n\nError on parsing stats for listing {id}.\n{e.GetBaseException()}");
            }

            listings.Add(listing);
        }

        return listings;
    }

    private async Task LoadListingStatsFromPage(string html, string id, ListingStats listing)
    {
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var imgSrc = htmlDoc.DocumentNode.SelectSingleNode($"//*[@id='mission-control-listing-stats']//a[contains(@href, '{id}')]//img[contains(@src, '170x135')]").Attributes["src"].Value;

        listing.TitlePhotoUrl = imgSrc;
        listing.Title = htmlDoc.DocumentNode.SelectSingleNode(ListingStatsPageXPaths.Title).InnerText.Trim();

        // General data

        var statsDataDropdown = "//div[contains(@class, 'stats-page-list')]/div/div/div[4]//div[contains(@class, 'dropdown')]//ul";
        listing.Visits = decimal.Parse(htmlDoc.DocumentNode.SelectSingleNode(@$"{statsDataDropdown}/li[1]/span/div").InnerText.Trim());
        listing.TotalViews = decimal.Parse(htmlDoc.DocumentNode.SelectSingleNode(@$"{statsDataDropdown}/li[2]/span/div").InnerText.Trim());
        listing.Orders = decimal.Parse(htmlDoc.DocumentNode.SelectSingleNode(@$"{statsDataDropdown}/li[3]/span/div").InnerText.Trim());
        listing.Revenue = decimal.Parse(htmlDoc.DocumentNode.SelectSingleNode(@$"{statsDataDropdown}/li[4]/span/div").InnerText.ExtractNumber());

        // Traffic sources

        var trafficSourcesList = "//div[contains(@class, 'stats-page-list')]//div/div/div[5]/div[1]/div[2]/div/div/div/div/div/ol";
        var trafficSource = $"{trafficSourcesList}//span[contains(text(), '{TextPlaceholder}')]/../../../../../div[2]/span[2]";

        var directAndOtherTraffic = htmlDoc.DocumentNode.SelectSingleNode(trafficSource.Replace(TextPlaceholder, "Direct &amp; other traffic")).InnerText;
        var etsyAppAndOtherEtsyPages = htmlDoc.DocumentNode.SelectSingleNode(trafficSource.Replace(TextPlaceholder, "Etsy app &amp; other Etsy pages")).InnerText;
        var etsyAds = htmlDoc.DocumentNode.SelectSingleNode(trafficSource.Replace(TextPlaceholder, "Etsy Ads")).InnerText;
        var etsyMarketingAndSeo = htmlDoc.DocumentNode.SelectSingleNode(trafficSource.Replace(TextPlaceholder, "Etsy marketing &amp; SEO")).InnerText;
        var socialMedia = htmlDoc.DocumentNode.SelectSingleNode(trafficSource.Replace(TextPlaceholder, "Social media")).InnerText;
        var etsySearch = htmlDoc.DocumentNode.SelectSingleNode(trafficSource.Replace(TextPlaceholder, "Etsy search")).InnerText;

        listing.DirectAndOtherTraffic = directAndOtherTraffic.ExtractNumber();
        listing.EtsyAppAndOtherEtsyPages = etsyAppAndOtherEtsyPages.ExtractNumber();
        listing.EtsyAds = etsyAds.ExtractNumber();
        listing.EtsyMarketingAndSeo = etsyMarketingAndSeo.ExtractNumber();
        listing.SocialMedia = socialMedia.ExtractNumber();
        listing.EtsySearch = etsySearch.ExtractNumber();

        // Search terms
        var searchTermTableXPath = "//table[@id='horizontal-chart4']";
        var searchTermRowXPath = $"{searchTermTableXPath}//tr[not(contains(@class,'column-header'))]";
        var searchTermXPath = "td/div/div[1]/div[1]/span[1]";
        var searchTermRows = htmlDoc.DocumentNode.SelectNodes(searchTermRowXPath);

        var searchTerms = new List<(string name, string totalVisits)>();
        if (searchTermRows is not null)
        {
            do
            {
                foreach (var searchTermRow in searchTermRows)
                {
                    var searchTerm = searchTermRow.SelectSingleNode(searchTermXPath).InnerText.Trim();

                    // Sometimes it's three columns (Etsy, Google, Total visits) in search terms, sometimes one (Visits)
                    var totalVisitsXPath =
                        // If 'Total visits' column exists
                        searchTermRow.SelectSingleNode($"{searchTermTableXPath}//div[contains(text(), 'Total visits')]") is not null
                            ? "td/div/div[1]/div[2]/div[3]" // path to 'Total visits cell
                            : "td/div/div[1]/div[2]"; // path to 'Visits' cell

                    var totalVisits = searchTermRow.SelectSingleNode(totalVisitsXPath).InnerText.Trim();
                    searchTerms.Add((searchTerm, totalVisits));
                }
            } while (await _websiteScrapingService.NextPage("{paginationXPath}//button[@title='Next page']", $"{searchTermRowXPath}/{searchTermXPath}"));

            listing.SearchTerms = string.Join(", ", searchTerms.Select(searchTerm => $"{searchTerm.name}: {searchTerm.totalVisits}"));
        }
    }

    private static ChromeDriver GetChromeDriver(string chromeLocation)
    {
        var options = new ChromeOptions
        {
            BinaryLocation = chromeLocation
        };

        options.AddArguments("headless");
        options.AddArguments("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
        options.AddArguments("--profile-directory=Пользователь 1");
        options.AddArguments(@"--user-data-dir=C:\Users\Administrator\AppData\Local\Google\Chrome\User Data");

        var chrome = new ChromeDriver(options);
        return chrome;
    }
}