using Microsoft.Extensions.Logging;
namespace QueuingDemo.Logic.Logging;

public class ColorConsoleLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = GetColor(logLevel);
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {formatter(state, exception)}");
        Console.ForegroundColor = originalColor;
    }

    private ConsoleColor GetColor(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => ConsoleColor.Gray,
        LogLevel.Debug => ConsoleColor.DarkGray,
        LogLevel.Information => ConsoleColor.Black,
        LogLevel.Warning => ConsoleColor.Red,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Critical => ConsoleColor.Magenta,
        _ => ConsoleColor.White
    };
}
