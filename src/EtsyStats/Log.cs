namespace EtsyStats;

// TODO use SeriLog
public static class Log
{
    public static async Task Error(string message)
    {
        await File.AppendAllTextAsync("logs/logs.txt", $"\r\n{DateTime.Now}:ERROR: {message}");
        await Debug(message);
    }

    public static async Task InfoAndConsole(string message)
    {
        await Info(message);
        Console.WriteLine($"\n{message}");
    }

    public static async Task Info(string message)
    {
        await File.AppendAllTextAsync("logs/logs.txt", $"\r\n{DateTime.Now}:INFO: {message}");
    }

    public static async Task Debug(string message)
    {
#if DEBUG
        Console.WriteLine($"\n{message}");
#endif
    }
}