using System.Text.RegularExpressions;

namespace EtsyStats.Extensions;

public static class StringExtensions
{
    private const string NumberPattern = @"(\d+(\.\d+)?)|(\.\d+)\d+";
    private const string SpecialCharactersPattern = "[^a-zA-Z0-9_.]+";

    public static string ExtractNumber(this string str)
    {
        return Regex.Match(str, NumberPattern).Value;
    }

    public static string ReplaceSpecialCharacters(this string str, string wordSeparator)
    {
        return Regex.Replace(str, SpecialCharactersPattern, wordSeparator, RegexOptions.Compiled);
    }
}