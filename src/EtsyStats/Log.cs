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
        await ProgramHelper.OriginalOut.WriteLineAsync($"\n{message}");
    }

    public static async Task Info(string message)
    {
        await File.AppendAllTextAsync("logs/logs.txt", $"\r\n{DateTime.Now}:INFO: {message}");
    }

    public static async Task Debug(string message)
    {
#if DEBUG
        await ProgramHelper.OriginalOut.WriteLineAsync($"\n{message}");
#endif
    }
}