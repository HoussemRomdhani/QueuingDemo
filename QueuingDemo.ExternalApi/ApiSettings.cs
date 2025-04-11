namespace QueuingDemo.ExternalApi;

public class ApiSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public int ItemCreationIntervalSeconds { get; set; } = 1;
}
