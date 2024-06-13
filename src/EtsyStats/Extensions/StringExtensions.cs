using System.Text.RegularExpressions;

namespace EtsyStats.Extensions;

public static class StringExtensions
{
    private const string NumberPattern = @"(\d+(\.\d+)?)|(\.\d+)\d+";
    private const string SpecialCharactersPattern = "[^a-zA-Z0-9_.]+";

    public static string ExtractNumber(this string str)
    {
        var matches = Regex.Matches(str, NumberPattern);

        return string.Join(string.Empty, matches.Select(m => m.Value));
    }

    public static string ReplaceSpecialCharacters(this string str, string wordSeparator)
    {
        return Regex.Replace(str, SpecialCharactersPattern, wordSeparator, RegexOptions.Compiled);
    }
}