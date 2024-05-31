using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;

namespace EtsyStats.Extensions;

public static class WebDriverExtensions
{
    private static readonly Random Random = new();

    public static async Task ClickWithDelay(this IWebDriver webDriver, string elementXPath)
    {
        var actions = new Actions(webDriver);
        await TaskDelay();
        actions.MoveToElement(webDriver.FindElement(By.XPath(elementXPath))).Click().Perform();
    }

    public static async Task NavigateToUrlWithDelay(this IWebDriver webDriver, string url)
    {
        await TaskDelay();
        webDriver.Navigate().GoToUrl(url);
    }

    private static async Task TaskDelay()
    {
        var milliseconds = Random.Next(Settings.MinDelayInMilliseconds, Settings.MaxDelayInMilliseconds);
        await Log.Debug($"\n Waiting {milliseconds / 1000} seconds \n");
        await Task.Delay(milliseconds);
    }
}