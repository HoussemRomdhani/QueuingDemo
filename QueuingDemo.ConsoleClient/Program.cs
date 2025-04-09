using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QueuingDemo.Logic;
using System.Threading;

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
            config.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<QueueProcessor>();
        services.AddSingleton(new ItemRepository("Server=(localdb)\\mssqllocaldb;Database=ReferencesConsumer;Integrated Security=True;"));

        var serviceProvider = services.BuildServiceProvider();
        
        //var queueProcessor = serviceProvider.GetRequiredService<QueueProcessor>();

        //await  queueProcessor.Work();

      //  var tasks = new List<Task>();

        Parallel.For(0, 10, async i =>
        {
            var queueProcessor = serviceProvider.GetRequiredService<QueueProcessor>();
            await queueProcessor.Work();
        });

        //for (int i = 0; i < 2; i++)
        //{
        //    // Get a new QueueProcessor instance from the service provider
        //    var queueProcessor = serviceProvider.GetRequiredService<QueueProcessor>();
        //    // Run each worker task concurrently
        //    tasks.Add(Task.Run(async () => await queueProcessor.Work()));
        //}

        //await Task.WhenAll(tasks);
        Console.ReadKey();
    }
}
