using EtsyStats.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace EtsyStats.Services;

public class WebScrapingService
{
    private readonly ChromeDriver _chromeDriver;

    public WebScrapingService(ChromeDriver chromeDriver)
    {
        _chromeDriver = chromeDriver;
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

    public async Task<string?> NavigateAndLoadHtmlFromUrl(string url, string elementToLoadXPath, string? errorElementXPath = null)
    {
        await _chromeDriver.NavigateToUrlWithDelay(url);
        var isErrorPage = WaitForElementToLoad(elementToLoadXPath, errorElementXPath);

        return isErrorPage ? null : _chromeDriver.PageSource;
    }

    private async Task<bool> ClickAndWaitForElementTextToChange(string buttonXpath, string elementXPath)
    {
        var initialText = _chromeDriver.FindElement(By.XPath(elementXPath)).Text;
        await _chromeDriver.ClickWithDelay(buttonXpath);

        return WaitForElementTextToChange(elementXPath, initialText);
    }

    private bool WaitForElementTextToChange(string elementXPath, string initialText)
    {
        var wait = new WebDriverWait(_chromeDriver, TimeSpan.FromSeconds(Settings.WaitDelayInSeconds));
        var textChanged = wait.Until(c => c.FindElement(By.XPath(elementXPath)).Text != initialText);

        return textChanged;
    }

    private bool WaitForElementToLoad(string elementToLoadXPath, string? errorElementXPath = null)
    {
        var wait = new WebDriverWait(_chromeDriver, TimeSpan.FromSeconds(Settings.WaitDelayInSeconds));
        wait.Until(c => c.FindElement(By.XPath(elementToLoadXPath))
                        ?? (errorElementXPath is not null ? c.FindElement(By.XPath(errorElementXPath)) : null));

        // TODO optimize - compare with HtmlDocument speed
        return _chromeDriver.FindElements(By.XPath(elementToLoadXPath)).Count > 0;
    }
}