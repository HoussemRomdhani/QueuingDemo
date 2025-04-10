using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueuingDemo.Logic;

namespace QueuingDemo.ConsoleClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddHttpClient<ApiService>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5165/api");
        });

        services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel( LogLevel.Information);
            config.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        });

        services.AddSingleton<QueueProcessor>();
        services.AddSingleton<ItemsRepository>();

        var serviceProvider = services.BuildServiceProvider();

        var cancellationTokenSource = new CancellationTokenSource();

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var queueProcessor = serviceProvider.GetRequiredService<QueueProcessor>();
            await queueProcessor.ProcessAsync(cancellationTokenSource.Token);
        }

        Console.ReadKey();
    }
}
