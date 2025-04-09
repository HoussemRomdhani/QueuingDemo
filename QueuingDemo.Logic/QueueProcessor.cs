using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
namespace QueuingDemo.Logic;

public class QueueProcessor
{
    private readonly ApiService apiService;
    private readonly ItemRepository itemRepository;
    private readonly ILogger<QueueProcessor> logger;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    public QueueProcessor(ApiService apiService, ItemRepository itemRepository, ILogger<QueueProcessor> logger)
    {
        this.apiService = apiService;
        this.itemRepository = itemRepository;
        this.logger = logger;
    }

    private async Task<string?> GetItemAsync(CancellationToken cancellationToken)
    {
        string? item = await apiService.GetItemAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(item))
        {
            await Task.Delay(3000, cancellationToken);
            await GetItemAsync(cancellationToken);
        }

        return item;
    }

    public async Task Work(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? item = await GetItemAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(item))
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    bool done = false;
                    while (!done)
                    {
                        done  = await itemRepository.ProcessItemAsync(item, Process, apiService.DeleteItemAsync, 5, cancellationToken);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
    }

    private async Task RemoveItem(string value, CancellationToken cancellationToken)
    {
        await itemRepository.TryDeleteAsync(value, cancellationToken);
        await apiService.DeleteItemAsync(value);
    }

    private void LogQueueContents(ConcurrentQueue<QueueItem> queue)
    {
        var items = queue.ToArray();
        logger.LogInformation("Queue has {Count} items", items.Length);
        foreach (var item in items)
        {
            logger.LogInformation(" - {Reference}, Attempts: {Attempts}", item.Reference, item.Attempts);
        }
    }

    private async Task<bool> Process(string value, CancellationToken cancellationToken)
    {
        await Task.Delay(500);
       // return await Task.FromResult(false);
         return await Task.FromResult(new Random().Next(2) == 0);
    }
}
