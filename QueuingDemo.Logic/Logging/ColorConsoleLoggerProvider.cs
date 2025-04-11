using Microsoft.Extensions.Logging;
namespace QueuingDemo.Logic.Logging;

public class ColorConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ColorConsoleLogger();
    }

    public void Dispose() { }
}
