using System.Text.RegularExpressions;

namespace EtsyStats.Extensions;

public static class StringExtensions
{
    private const string NumberPattern = @"(\d+(\.\d+)?)|(\.\d+)\d+";

    public static string ExtractNumber(this string s)
    {
        return Regex.Match(s, NumberPattern).Value;
    }
}