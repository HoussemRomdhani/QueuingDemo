namespace QueuingDemo.ExternalApi;

public class TimedHostedService : IHostedService, IDisposable
{
    private readonly ItemRepository itemRepository;
    private readonly ILogger<TimedHostedService> logger;
    private Timer? timer;
    public TimedHostedService(ItemRepository itemRepository, ILogger<TimedHostedService> logger)
    {
        this.itemRepository = itemRepository;
        this.logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Timed Hosted Service is starting.");

        // Set the timer to run every 5 seconds (5000 ms)
        timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

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

        // Dispose of the timer
        timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        timer?.Dispose();
    }
}
