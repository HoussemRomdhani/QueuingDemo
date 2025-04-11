using Dapper;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace QueuingDemo.ExternalApi;

public class ItemsRepository
{
    private readonly IOptionsMonitor<ApiSettings> apiSettings;
    public ItemsRepository(IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.apiSettings = apiSettings;
    }

    public async Task<Item?> Get()
    {
        using (var connection = new SqlConnection(apiSettings.CurrentValue.ConnectionString))
        {
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<Item>("SELECT TOP 1 Id, Value FROM Items ORDER BY 1");
        }
    }

    public async Task CreateAsync(string item)
    {
        using (var connection = new SqlConnection(apiSettings.CurrentValue.ConnectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("INSERT INTO Items (Value) OUTPUT INSERTED.Id VALUES (@Value)", new { Value = item });
        }
    }

    public async Task<bool> DeleteAsync(string value)
    {
        using (var connection = new SqlConnection(apiSettings.CurrentValue.ConnectionString))
        {
            await connection.OpenAsync();
            int result =  await connection.ExecuteAsync("DELETE FROM Items WHERE Value = @Value", new { Value = value });
            return result > 0;
        }
    }
}


