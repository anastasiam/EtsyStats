namespace EtsyStats;

// TODO use SeriLog
public static class Log
{
    public static async Task Error(string message)
    {
        var nextLineMessage = $"\r\n{message}";
        await File.AppendAllTextAsync("logs/logs.txt",$"{DateTime.Now}: ----- {nextLineMessage}");
#if DEBUG
        await ProgramHelper.OriginalOut.WriteLineAsync(nextLineMessage);
#endif
    }
}