namespace EtsyStats;

// TODO use SeriLog
public static class Log
{
    public static async Task Error(string message)
    {
        var nextLineMessage = $"\r\n{message}";
        await File.AppendAllTextAsync("logs/logs.txt", $"\n\r{DateTime.Now}:ERROR:{nextLineMessage}");
        await Debug(nextLineMessage);
    }

    public static async Task InfoAndConsole(string message)
    {
        await Info(message);
        await ProgramHelper.OriginalOut.WriteLineAsync(message);
    }

    public static async Task Info(string message)
    {
        var nextLineMessage = $"\r\n{message}";
        await File.AppendAllTextAsync("logs/logs.txt", $"\n\r{DateTime.Now}:INFO:{nextLineMessage}");
    }

    public static async Task Debug(string message)
    {
#if DEBUG
        await ProgramHelper.OriginalOut.WriteLineAsync(message);
#endif
    }
}