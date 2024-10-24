using EtsyStats.Models.Options;
using EtsyStats.Services;
using Selenium.Extensions;

namespace EtsyStats.Factories;

public class WebScrapingServiceFactory
{
    public WebScrapingService Create(SlDriver driver, ConfigurationOptions options)
    {
        return new WebScrapingService(driver, options);
    }
}