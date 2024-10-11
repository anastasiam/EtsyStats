using EtsyStats.Constants.Etsy;
using EtsyStats.Extensions;
using EtsyStats.Helpers;
using EtsyStats.Models;
using EtsyStats.Models.Enums;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Selenium.Extensions;
using Selenium.WebDriver.UndetectedChromeDriver;
using Sl.Selenium.Extensions.Chrome;

namespace EtsyStats.Services;

public class EtsyParser
{
    private const string UserDataDirectory = @"Google\Chrome\User Data";
    private const string LocalUserDataDir = @"ChromeUserData";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36";
    
    private const string CategorySeparator = " / ";

    private const string Href = "href";
    private const string Src = "src";
    private const string InnerText = "innerText";
    private const string Value = "value";
    
    private readonly DirectoryHelper _directoryHelper;

    public EtsyParser(DirectoryHelper directoryHelper)
    {
        _directoryHelper = directoryHelper;
    }

    public async Task<List<ListingStats>> GetListingsStats(DateRange dateRange)
    {
        using var chromeDriver = GetUndetectedChromeDriver();
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
            Console.WriteLine($"\nProcessing listing {i + 1} out of  {listingsIds.Count}...\n");

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
                Console.WriteLine($"\nAn error occured while parsing listing {id}.");
                await Log.Error($"Exception on parsing stats for listing {id}.\r\n{e}");
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

    private async Task LoadListingFromEditPage(ISearchContext chromeDriver, WebScrapingService webScrapingService, string id, ListingStats listing)
    {
        var url = EtsyUrl.ListingEdit(id);
        await webScrapingService.NavigateAndLoadHtmlFromUrl(url, ListingEditPageXPaths.ShopSection);

        var tags = chromeDriver.FindElements(By.XPath(ListingEditPageXPaths.Tag));
        var categories = chromeDriver.FindElements(By.XPath(ListingEditPageXPaths.Category));
        var shopSectionSelector = new SelectElement(chromeDriver.FindElement(By.XPath(ListingEditPageXPaths.ShopSection)));
        var listedDate = chromeDriver.FindElements(By.XPath(ListingEditPageXPaths.ListedDate));
        var sku = chromeDriver.FindElements(By.XPath(ListingEditPageXPaths.Sku));
        var shippingProfile = chromeDriver.FindElements(By.XPath(ListingEditPageXPaths.ShippingProfile));

        if (tags is not null)
            listing.Tags = tags.Select(t => t.GetAttribute(InnerText).Trim()).ToList();

        if (categories is not null)
            listing.Category = string.Join(CategorySeparator, categories.Select(c => c.GetAttribute(InnerText).Trim()));

        if (listedDate is not null)
            listing.ListedDate = listedDate.FirstOrDefault()?.GetAttribute(InnerText).Trim().ExtractDate();

        if (sku is not null)
            listing.Sku = sku.FirstOrDefault()?.GetAttribute(Value).Trim();

        if (shippingProfile is not null)
            listing.ShippingProfile = shippingProfile.FirstOrDefault()?.GetAttribute(InnerText).Trim();
        
        listing.ShopSection = shopSectionSelector.SelectedOption.GetAttribute(InnerText);
    }

    private void ParseStatsPageGeneralData(ISearchContext chromeDriver, ListingStats listing)
    {
        var statsDataDropdown = chromeDriver.FindElement(By.XPath(ListingStatsPageXPaths.GeneralDataDropdown));

        listing.Visits = decimal.Parse(statsDataDropdown.FindElement(By.XPath(ListingStatsPageXPaths.VisitsDropdownElement)).GetAttribute(InnerText).Trim());
        listing.TotalViews = decimal.Parse(statsDataDropdown.FindElement(By.XPath(ListingStatsPageXPaths.TotalViewsDropdownElement)).GetAttribute(InnerText).Trim());
        listing.Orders = decimal.Parse(statsDataDropdown.FindElement(By.XPath(ListingStatsPageXPaths.OrdersDropdownElement)).GetAttribute(InnerText).Trim());
        listing.Revenue = decimal.Parse(statsDataDropdown.FindElement(By.XPath(ListingStatsPageXPaths.RevenueDropdownElement)).GetAttribute(InnerText).ExtractNumber());
    }

    private void ParseStatsPageTrafficSources(ISearchContext chromeDriver, ListingStats listing)
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

    private SlDriver GetUndetectedChromeDriver()
    {
        var localUserDataDir = CopyChromeUserDataDirectory();

        var chromeDriversParameters = new ChromeDriverParameters
        {
            DriverArguments = new HashSet<string>()
        };

        chromeDriversParameters.DriverArguments.Add($"--user-agent={UserAgent}");
        chromeDriversParameters.DriverArguments.Add($"--user-data-dir={localUserDataDir}");
        chromeDriversParameters.DriverArguments.Add("--no-sandbox");
        chromeDriversParameters.DriverArguments.Add("--profile-directory=Default");
        
        return UndetectedChromeDriver.Instance(chromeDriversParameters);
    }

    private string CopyChromeUserDataDirectory()
    {
        var localUserDataDirFullName = new DirectoryInfo(LocalUserDataDir).FullName;

        if (Directory.Exists(localUserDataDirFullName))
        {
            Directory.Delete(localUserDataDirFullName, true);
        }

        var userDataDir =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}/{UserDataDirectory}";

        _directoryHelper.CopyAll(userDataDir, localUserDataDirFullName);

        return localUserDataDirFullName;
    }

}