using QueuingDemo.Logic;

public class HostedService : BackgroundService
{
    private readonly ILogger<HostedService> logger;
    private readonly QueueProcessor processor;
    public HostedService(QueueProcessor processor, ILogger<HostedService> logger)
    {
        this.processor = processor;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("🚀 Consumer Hosted Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
             await processor.ProcessAsync(stoppingToken);
        }

        logger.LogError("🛑 QueueWorker is stopping, cleaning up...");
        
        await processor.CleanWorkingData();
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogError("🛑 QueueWorker is stopping, cleaning up...");
     
        await processor.CleanWorkingData();
       
        await base.StopAsync(stoppingToken);
    }
}