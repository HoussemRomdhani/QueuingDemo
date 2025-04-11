namespace QueuingDemo.Logic.Settings;

public class ClientSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ApiServiceBaseAddress { get; set; } = string.Empty;
    public int MaxAttempts { get; set; } = 5;
    public int NotFoundWaitTimeMilliseconds { get; set; } = 3000;
    public int ProcessedSuccessfullyPercentage { get; set; } = 50;
    public ProcessingIntervalMilliseconds ProcessingIntervalMilliseconds { get; set; } = new();
}
