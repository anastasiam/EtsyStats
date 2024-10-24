using EtsyStats.Models.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Selenium.Extensions;

namespace EtsyStats.Services;

public class WebScrapingService : IDisposable
{
    private readonly SlDriver _chromeDriver;
    private readonly ConfigurationOptions _configurationOptions;

    public WebScrapingService(SlDriver chromeDriver,
        ConfigurationOptions configurationOptions)
    {
        _chromeDriver = chromeDriver;
        _configurationOptions = configurationOptions;
    }

    public async Task<bool> NextPage(string paginationXPath, string tableCellXPath)
    {
        var nextButtonXPath = $"{paginationXPath}//button[@title='Next page']";
        var nextButtons = _chromeDriver.FindElements(By.XPath(nextButtonXPath));
        if (nextButtons.Count == 0 || !nextButtons[0].Enabled)
        {
            return false;
        }

        return await ClickAndWaitForElementTextToChange(nextButtonXPath, tableCellXPath);
    }

    public async Task<bool> NavigateAndLoadHtmlFromUrl(string url, string elementToLoadXPath, string? errorElementXPath = null)
    {
        await NavigateToUrlWithDelay(url);
        var pageLoadedSuccessfully = WaitForElementToLoad(elementToLoadXPath, errorElementXPath);

        return pageLoadedSuccessfully;
    }

    public T HandleStaleElements<T>(Func<T> func)
    {
        var wait = new WebDriverWait(_chromeDriver, TimeSpan.FromSeconds(_configurationOptions.WaitDelayInSeconds));
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));

        return wait.Until(_ => func());
    }

    public void Dispose()
    {
        _chromeDriver.Dispose();
        GC.SuppressFinalize(_chromeDriver);
    }

    private async Task<bool> ClickAndWaitForElementTextToChange(string buttonXpath, string elementXPath)
    {
        var initialText = _chromeDriver.FindElement(By.XPath(elementXPath)).Text;
        await ClickWithDelay(buttonXpath);

        return WaitForElementTextToChange(elementXPath, initialText);
    }

    private bool WaitForElementTextToChange(string elementXPath, string initialText)
    {
        var wait = new WebDriverWait(_chromeDriver, TimeSpan.FromSeconds(_configurationOptions.WaitDelayInSeconds));
        var textChanged = wait.Until(c => c.FindElement(By.XPath(elementXPath)).Text != initialText);

        return textChanged;
    }

    private bool WaitForElementToLoad(string elementToLoadXPath, string? errorElementXPath = null)
    {
        var wait = new WebDriverWait(_chromeDriver, TimeSpan.FromSeconds(_configurationOptions.WaitDelayInSeconds));
        
        var result = wait.Until(c =>
        {
            var el = c.FindElements(By.XPath(elementToLoadXPath));
            if (el.Count > 0 && el.All(e => e.Displayed && e.Enabled)) return true;

            return errorElementXPath is not null && c.FindElement(By.XPath(errorElementXPath)) != null;
        });

        return result && _chromeDriver.FindElements(By.XPath(elementToLoadXPath)).Count > 0;
    }

    private async Task ClickWithDelay(string elementXPath)
    {
        var actions = new Actions(_chromeDriver);
        await TaskDelay();
        actions.MoveToElement(_chromeDriver.FindElement(By.XPath(elementXPath))).Click().Perform();
    }

    private async Task NavigateToUrlWithDelay(string url)
    {
        await TaskDelay();
        _chromeDriver.Navigate().GoToUrl(url);
    }

    private async Task TaskDelay()
    {
        var milliseconds = new Random().Next(_configurationOptions.MinDelayInMilliseconds, _configurationOptions.MaxDelayInMilliseconds);
        await Task.Delay(milliseconds);
    }
}