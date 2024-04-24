namespace EtsyStats;

public static class Startup
{
    public static void SetupLogs()
    {
        Directory.CreateDirectory("logs");
        File.WriteAllText("logs/logs.txt", $"\n\n------------{DateTime.Now}------------");
    }
}