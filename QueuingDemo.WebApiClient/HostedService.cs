using QueuingDemo.Logic;

public class HostedService : IHostedService
{
    private readonly ILogger<HostedService> logger;
    private readonly QueueProcessor processor;
    public HostedService(QueueProcessor processor, ILogger<HostedService> logger)
    {
        this.processor = processor;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Consumer Hosted Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);
            await  processor.ProcessAsync(stoppingToken);
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Timed Hosted Service is stopping.");
        return Task.CompletedTask;
    }
}