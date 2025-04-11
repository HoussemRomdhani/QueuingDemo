using Microsoft.Extensions.Options;

namespace QueuingDemo.ExternalApi;

public class HostedService : IHostedService, IDisposable
{
    private readonly ItemsRepository itemRepository;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;
    private readonly ILogger<HostedService> logger;
    private Timer? timer;
    public HostedService(ItemsRepository itemRepository, IOptionsMonitor<ApiSettings> apiSettings, ILogger<HostedService> logger)
    {
        this.itemRepository = itemRepository;
        this.apiSettings = apiSettings;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Timed Hosted Service is starting.");

        timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(apiSettings.CurrentValue.ItemCreationIntervalSeconds));

        // React to config changes
        apiSettings.OnChange(settings =>
        {
            logger.LogInformation("Configuration changed. New interval: {Interval} seconds", settings.ItemCreationIntervalSeconds);

            // Change timer interval on the fly
            timer?.Change(
                TimeSpan.Zero,
                TimeSpan.FromSeconds(settings.ItemCreationIntervalSeconds)
            );
        });

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        logger.LogInformation("Timed Hosted Service is working. {Time}", DateTimeOffset.Now);
        try
        {
            string value = GenerateRandomAlphanumeric();
            await itemRepository.CreateAsync(value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while performing background task.");
        }
    }

    private static string GenerateRandomAlphanumeric(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length).Select(__ => chars[random.Next(chars.Length)]).ToArray());
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Timed Hosted Service is stopping.");

        timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        timer?.Dispose();
    }
}
