namespace EtsyStats.Helpers;

public static class GoogleSheetsFormula
{
    public static string? GetImageFromUrl(string? imageUrl)
    {
        return imageUrl is null ? null : $"=IMAGE(\"{imageUrl}\")";
    }
}