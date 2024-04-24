namespace EtsyStats.Constants.Etsy;

public static class ListingsPageXPaths
{
    public const string ListingsListElement = "//div[contains(@class, 'list-region')]/div/div/ul/li";
    public const string ListingLink = "div/a";
    public const string EmptyStateDiv = $"//div[contains(@class, 'empty-state')]";
}