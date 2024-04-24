namespace EtsyStats.Constants.Etsy;

public static class SearchAnalyticsPageXPaths
{
    public const string Table = "//*[@id='main-content']/div/div[3]/div/table";
    public const string TableRow = $"{Table}/tbody/tr";
    public const string SearchQueryFirstTableCellFullXPath = $"{TableRow}[1]/{SearchQueryTableCell}";

    public const string SearchQueryTableCell = "td[1]/a/span[2]";
    public const string ImpressionsTableCell = "td[2]";
    public const string PositionTableCell = "td[3]";
    public const string VisitsTableCell = "td[4]";
    public const string ConversionRateTableCell = "td[5]";
    public const string RevenueTableCell = "td[6]";
    public const string ListingsTableCell = "td[7]/span/a";
}