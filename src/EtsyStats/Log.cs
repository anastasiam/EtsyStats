namespace EtsyStats;

// TODO use SeriLog
public static class Log
{
    public static async Task Error(string message)
    {
        var nextLineMessage = $"\r\n{message}";
        await File.AppendAllTextAsync("logs/logs.txt",$"{DateTime.Now}: ----- {nextLineMessage}");
        await Debug(nextLineMessage);
    }

    public static async Task Debug(string message)
    {
#if DEBUG
        await ProgramHelper.OriginalOut.WriteLineAsync(message);
#endif
    }
}