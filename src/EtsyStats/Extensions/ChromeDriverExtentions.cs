using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;

namespace EtsyStats.Extensions;

public static class ChromeDriverExtensions
{
    public static async Task ClickWithDelay(this ChromeDriver chromeDriver, string elementXPath)
    {
        var actions = new Actions(chromeDriver);
        await TaskDelay();
        actions.MoveToElement(chromeDriver.FindElement(By.XPath(elementXPath))).Click().Perform();
    }

    public static async Task NavigateToUrlWithDelay(this ChromeDriver chromeDriver, string url)
    {
        await TaskDelay();
        chromeDriver.Navigate().GoToUrl(url);
    }

    private static async Task TaskDelay()
    {
        await Task.Delay(new Random().Next(Settings.MinDelayBeforeActionInSeconds, Settings.MaxDelayBeforeActionInSeconds));
    }
}