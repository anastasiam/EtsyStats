namespace EtsyStats;

// TODO use SeriLog
public static class Log
{
    public static async Task ErrorAsync(string message)
    {
#if DEBUG
        Console.WriteLine($"\n[ERROR]: {message}");
#endif
        await File.AppendAllTextAsync("logs/logs.txt", $"\r\n{DateTime.Now}:[ERROR]: {message}");
    }

    public static async Task InfoAndConsoleAsync(string message)
    {
        await File.AppendAllTextAsync("logs/logs.txt", $"\r\n{DateTime.Now}:[INFO]: {message}");
        Console.WriteLine($"\n{message}");
    }

    public static async Task InfoAsync(string message)
    {
#if DEBUG
        Console.WriteLine($"\n[INFO]: {message}");
#endif
        await File.AppendAllTextAsync("logs/logs.txt", $"\r\n{DateTime.Now}:[INFO]: {message}");
    }

    public static async Task DebugAsync(string message)
    {
#if DEBUG
        Console.WriteLine($"\n[DEBUG]: {message}");
        await File.AppendAllTextAsync("logs/logs.txt", $"\r\n{DateTime.Now}:[DEBUG]: {message}");
#endif
    }
}