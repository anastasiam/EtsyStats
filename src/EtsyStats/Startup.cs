namespace EtsyStats;

public static class Startup
{
    public static void SetupLogs()
    {
        Directory.CreateDirectory("logs");
        File.AppendAllText("logs/logs.txt", $"\r\n------------{DateTime.Now}------------\r\n");
    }
}