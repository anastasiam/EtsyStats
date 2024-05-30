using EtsyStats.Models.Enums;

namespace EtsyStats;

public static class ProgramHelper
{
    public static TextWriter OriginalOut;

    public static DateRange GetDateRange()
    {
        OriginalOut.WriteLine("\nPlease select the date range:");
        OriginalOut.WriteLine("1 - Last 30 days");
        OriginalOut.WriteLine("2 - This month");
        OriginalOut.WriteLine("3 - Last month");
        OriginalOut.WriteLine("4 - This year");
        OriginalOut.WriteLine("5 - Last year");
        var dateRangeCommand = Console.ReadLine();

        switch (dateRangeCommand)
        {
            case "1":
                OriginalOut.WriteLine("\nGetting data for last 30 days");
                return DateRange.Last30Days;
            case "2":
                OriginalOut.WriteLine("\nGetting data for this month");
                return DateRange.ThisMonth;
            case "3":
                OriginalOut.WriteLine("\nGetting data for last month");
                return DateRange.LastMonth;
            case "4":
                OriginalOut.WriteLine("\nGetting data for this year");
                return DateRange.ThisYear;
            case "5":
                OriginalOut.WriteLine("\nGetting data for last year");
                return DateRange.LastYear;
            default:
                OriginalOut.WriteLine("\nUnknown command, please try again.");
                GetDateRange();
                break;
        }

        return DateRange.Unknown;
    }

    public static DateRange GetDateRangeStats()
    {
        OriginalOut.WriteLine("\nPlease select the date range:");
        OriginalOut.WriteLine("1 - Last 30 days");
        OriginalOut.WriteLine("2 - This month");
        OriginalOut.WriteLine("3 - This year");
        OriginalOut.WriteLine("4 - Last year");
        OriginalOut.WriteLine("5 - All time");
        var dateRangeCommand = Console.ReadLine();

        switch (dateRangeCommand)
        {
            case "1":
                OriginalOut.WriteLine("\nGetting data for last 30 days");
                return DateRange.Last30Days;
            case "2":
                OriginalOut.WriteLine("\nGetting data for this month");
                return DateRange.ThisMonth;
            case "3":
                OriginalOut.WriteLine("\nGetting data for this year");
                return DateRange.ThisYear;
            case "4":
                OriginalOut.WriteLine("\nGetting data for last year");
                return DateRange.LastYear;
            case "5":
                OriginalOut.WriteLine("\nGetting data for all time");
                return DateRange.AllTime;
            default:
                OriginalOut.WriteLine("\nUnknown command, please try again.");
                GetDateRange();
                break;
        }

        return DateRange.Unknown;
    }
}