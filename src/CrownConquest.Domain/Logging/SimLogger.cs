namespace CrownConquest.Domain.Logging;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public interface ILogSink
{
    void Log(LogLevel level, string category, string message);
}

public sealed class ConsoleLogSink : ILogSink
{
    public void Log(LogLevel level, string category, string message)
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] [{level}] [{category}] {message}");
    }
}

public sealed class InMemoryLogSink : ILogSink
{
    private readonly List<string> _logs = new(256);
    private readonly object _lock = new();

    public IReadOnlyList<string> Logs
    {
        get
        {
            lock (_lock)
            {
                return _logs.ToArray();
            }
        }
    }

    public void Log(LogLevel level, string category, string message)
    {
        lock (_lock)
        {
            _logs.Add($"[{level}] [{category}] {message}");
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _logs.Clear();
        }
    }
}

public static class SimLogger
{
    private static readonly List<ILogSink> Sinks = new();
    private static readonly object Lock = new();

    public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;

    public static void AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (Lock)
        {
            if (!Sinks.Contains(sink))
            {
                Sinks.Add(sink);
            }
        }
    }

    public static void RemoveSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (Lock)
        {
            Sinks.Remove(sink);
        }
    }

    public static void ClearSinks()
    {
        lock (Lock)
        {
            Sinks.Clear();
        }
    }

    public static void LogDebug(string category, string message) => Log(LogLevel.Debug, category, message);
    public static void LogInfo(string category, string message) => Log(LogLevel.Info, category, message);
    public static void LogWarning(string category, string message) => Log(LogLevel.Warning, category, message);
    public static void LogError(string category, string message) => Log(LogLevel.Error, category, message);

    public static void Log(LogLevel level, string category, string message)
    {
        if (level < MinimumLevel) return;

        lock (Lock)
        {
            int count = Sinks.Count;
            for (int i = 0; i < count; i++)
            {
                Sinks[i].Log(level, category, message);
            }
        }
    }
}
