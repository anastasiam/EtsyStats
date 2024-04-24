using System.Web;
using EtsyStats.Models.Enums;

namespace EtsyStats.Helpers;

public static class DateRangeQueryParameter
{
    private const string StartDatePlaceholder = "{startDate}";
    private const string EndDatePlaceholder = "{endDate}";
    private const string DateFormat = "MM/dd/yyyy";
    private const string ThisMonth = "dateRange=this-month";
    private const string LastMonth = "dateRange=last-month";
    private const string ThisYear = "dateRange=this-year";
    private const string LastYear = "dateRange=last-year";
    private const string Last30Days = $"dateRange=undefined&startDate={StartDatePlaceholder}&endDate={EndDatePlaceholder}";

    public static string? GetQuery(DateRange dateRange)
    {
        switch (dateRange)
        {
            case DateRange.ThisMonth:
                return ThisMonth;
            case DateRange.LastMonth:
                return LastMonth;
            case DateRange.ThisYear:
                return ThisYear;
            case DateRange.LastYear:
                return LastYear;
            case DateRange.Last30Days:
                var startDate = HttpUtility.UrlEncode(DateTime.Now.AddDays(-30).ToString(DateFormat));
                var endDate = HttpUtility.UrlEncode(DateTime.Now.ToString(DateFormat));

                return Last30Days.Replace(StartDatePlaceholder, startDate).Replace(EndDatePlaceholder, endDate);
            case DateRange.Unknown:
            default:
                return null;
        }
    }
}