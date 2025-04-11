using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueuingDemo.Logic.Models;
using QueuingDemo.Logic.Settings;
using System.Diagnostics;
namespace QueuingDemo.Logic;


public class QueueProcessor
{
    private readonly ApiService apiService;
    private readonly ItemsRepository itemRepository;
    private readonly IOptionsMonitor<ClientSettings> clientSettings;
    private readonly ILogger<QueueProcessor> logger;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    public QueueProcessor(ApiService apiService, ItemsRepository itemRepository, IOptionsMonitor<ClientSettings> clientSettings, ILogger<QueueProcessor> logger)
    {
        this.apiService = apiService;
        this.itemRepository = itemRepository;
        this.clientSettings = clientSettings;
        this.logger = logger;
    }

    void LogColored(string message, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var taskId = Guid.NewGuid();

        bool locked = false;

        try
        {
            locked = await semaphore.WaitAsync(Timeout.Infinite, cancellationToken);

            string? value = await apiService.GetItemAsync(cancellationToken);

            if (value == null)
            {
                return;
            }

            bool success = await itemRepository.InsertAndLockItemIfNewOrIncrementAttemptsAsync(value, taskId, cancellationToken);

            if (!success)
            {
                return;
            }

            var itemAttempts = await itemRepository.GetItemAttemptsAsync(value, taskId, cancellationToken);

            if (itemAttempts != null)
            {
                int maxAttempts = clientSettings.CurrentValue.MaxAttempts;

                clientSettings.OnChange(options =>
                {
                    maxAttempts = options.MaxAttempts;
                });

                if (itemAttempts.Attempts > maxAttempts)
                {
                    bool deletedFromExternal = await apiService.DeleteItemAsync(value, cancellationToken);
                    if (deletedFromExternal)
                    {
                        await itemRepository.DeleteItemAttemptsAsync(value, cancellationToken);
                    }
                }
                else
                {
                    bool precessed = await ProcessItemAsync(value, itemAttempts.Attempts, cancellationToken);

                    if (precessed)
                    {
                        bool deletedFromExternal = await apiService.DeleteItemAsync(value, cancellationToken);

                        if (deletedFromExternal)
                        {
                            await itemRepository.DeleteItemAttemptsAsync(value, cancellationToken);
                        }
                    }
                }

                await itemRepository.DeleteItemAsync(value, taskId, cancellationToken);
            }
            else
            {
                logger.LogCritical("[{taskId}] was locked : {value}", taskId, value);
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

    private async Task<bool> ProcessItemAsync(string value, int attempt, CancellationToken cancellationToken)
    {
        ProcessingIntervalMilliseconds processingIntervalMilliseconds = clientSettings.CurrentValue.ProcessingIntervalMilliseconds;
       
        int processedSuccessfullyPercentage = clientSettings.CurrentValue.ProcessedSuccessfullyPercentage;

        clientSettings.OnChange(options =>
        {
            processingIntervalMilliseconds = options.ProcessingIntervalMilliseconds;
            processedSuccessfullyPercentage = options.ProcessedSuccessfullyPercentage;
        });

        int totalProcessingTimeMs = new Random().Next(processingIntervalMilliseconds.Min, processingIntervalMilliseconds.Max);  // 10 seconds (total time for the operation)

        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("▶️ Processing started - Attempt N° {attempt} : {value}", attempt, value);

        while (stopwatch.ElapsedMilliseconds < totalProcessingTimeMs)
        {
            double elapsedMs = stopwatch.ElapsedMilliseconds;
            int progressPercentage = (int)((elapsedMs / totalProcessingTimeMs) * 100);

            progressPercentage = Math.Min(progressPercentage, 100);

            logger.LogTrace("⏳ Processing {value} ... {progressPercentage}%", value, progressPercentage);

            await Task.Delay(1000, cancellationToken);
        }

        int random = new Random().Next(100);

        bool result = random < processedSuccessfullyPercentage ? await Task.FromResult(true) : await Task.FromResult(false);

        if (result)
        {
            logger.LogInformation("✅ Processing completed successfully - Attempt N° {attempt} : {value}", attempt, value);
        }
        else
        {
            logger.LogWarning("⚠️ Processing failed -  Attempt N° {attempt} : {value}", attempt, value);
        }

        stopwatch.Stop();
        return result;
    }

    public async Task CleanWorkingData() => await itemRepository.CleanWorkingData();
}
