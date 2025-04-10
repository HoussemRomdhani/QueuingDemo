using Microsoft.Extensions.Logging;
namespace QueuingDemo.Logic;

public class QueueProcessor
{
    private readonly ApiService apiService;
    private readonly ItemsRepository itemRepository;
    private readonly ILogger<QueueProcessor> logger;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    public QueueProcessor(ApiService apiService, ItemsRepository itemRepository, ILogger<QueueProcessor> logger)
    {
        this.apiService = apiService;
        this.itemRepository = itemRepository;
        this.logger = logger;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var taskId = Guid.NewGuid();

        bool locked = false;

        try
        {
            locked = await semaphore.WaitAsync(Timeout.Infinite, cancellationToken);

            string value = await GetItemAsync(cancellationToken);

            logger.LogInformation("[{taskId}] Working on item : {value}", taskId, value);

            if (string.Empty.Equals(value.Trim()))
            {
                await apiService.DeleteItemAsync(value, cancellationToken);
                logger.LogInformation("[{taskId}] Work finished on item : EMPTY", taskId);
            }
            else
            {
                Result done = Result.Fail;

                while (done == Result.Fail)
                {
                    done = await itemRepository.ProcessItemAsync(value, taskId, Process, apiService.DeleteItemAsync, 5, cancellationToken);
                }

                if (done == Result.Success)
                {
                    logger.LogInformation("[{taskId}] Work finished on item : {value}", taskId, value);
                }

                if (done == Result.Ignore)
                {
                    logger.LogInformation("[{taskId}] Work ignored on item : {value}", taskId, value);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "[{taskId}] Something went wrong: {message}", taskId, e.Message);
        }
        finally
        {
            if (locked)
            {
                semaphore.Release();
            }
        }
    }

    private async Task<string> GetItemAsync(CancellationToken cancellationToken)
    {
        string? item = await apiService.GetItemAsync(cancellationToken);

        if (item != null)
        {
            return item;
        }

        await Task.Delay(3000, cancellationToken);

        return await GetItemAsync(cancellationToken);
    }

    private async Task<bool> Process(string value, CancellationToken cancellationToken)
    {
        await Task.Delay(new Random().Next(500, 10000), cancellationToken);
        return await Task.FromResult(new Random().Next(100) <= 40);
    }
}
