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

    private const string TodayStats = "date_range=today";
    private const string YesterdayStats = "date_range=yesterday";
    private const string Last7DaysStats = "date_range=last_7";
    private const string Last30DaysStats = "date_range=last_30";
    private const string ThisMonthStats = "date_range=this_month";
    private const string ThisYearStats = "date_range=this_year";
    private const string LastYearStats = "date_range=last_year";
    private const string AllTimeStats = "?date_range=all_time";

    public static string? GetQueryAnalytics(DateRange dateRange)
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

    public static string? GetQueryStats(DateRange dateRange)
    {
        switch (dateRange)
        {
            case DateRange.Today:
                return TodayStats;
            case DateRange.Yesterday:
                return YesterdayStats;
            case DateRange.Last7Days:
                return Last7DaysStats;
            case DateRange.Last30Days:
                return Last30DaysStats;
            case DateRange.ThisMonth:
                return ThisMonthStats;
            case DateRange.ThisYear:
                return ThisYearStats;
            case DateRange.LastYear:
                return LastYearStats;
            case DateRange.AllTime:
                return AllTimeStats;
            case DateRange.Unknown:
            default:
                return null;
        }
    }
}