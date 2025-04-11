using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using QueuingDemo.Logic.Models;
using QueuingDemo.Logic.Settings;
using System.Data.SqlClient;
namespace QueuingDemo.Logic;

public class ItemsRepository
{
    private readonly IOptionsMonitor<ClientSettings> clientSettings;
    private readonly ILogger<ItemsRepository> logger;
    public ItemsRepository(IOptionsMonitor<ClientSettings> clientSettings, ILogger<ItemsRepository> logger)
    {
        this.clientSettings = clientSettings;
        this.logger = logger;
    }

    public async Task<bool> InsertAndLockItemIfNewOrIncrementAttemptsAsync(string value, Guid taskId, CancellationToken cancellationToken = default)
    {
        const string query = @" IF NOT EXISTS (SELECT 1 FROM Items WHERE Value = @Value)
                                BEGIN
                                      INSERT INTO Items (Value, Lock) VALUES (@Value, @Lock)
                                    
                                   IF NOT EXISTS (SELECT 1 FROM ItemAttempts WHERE Value = @Value)
                                   BEGIN
                                         INSERT INTO ItemAttempts (Value) VALUES (@Value)
                                   END
                                   ELSE
                                    BEGIN
                                        UPDATE ItemAttempts SET Attempts = Attempts + 1 WHERE Value = @Value
                                    END
                                END";

        var policy = Policy.Handle<SqlException>(ex => ex.Number == 2627).WaitAndRetryAsync(1, _ => TimeSpan.FromSeconds(1)); 

        using (var connection = new SqlConnection(clientSettings.CurrentValue.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    bool result = await policy.ExecuteAsync(async () =>
                    {
                        return (await connection.ExecuteAsync(query, new { Value = value, Lock = taskId }, transaction)) > 0;
                    });

                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch (SqlException ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    logger.LogError(ex, "⚠️ An error occurred while inserting or updating item attempts for value: {Value}", value);
                    return false;
                }
            }
        }
    }

    public async Task<ItemAttempts?> GetItemAttemptsAsync(string value, Guid taskId, CancellationToken cancellationToken = default)
    {
        const string query = @$"SELECT IA.* 
                                       FROM Items I
                                       JOIN ItemAttempts IA ON I.Value = IA.Value
                                       WHERE I.Value = @Value AND I.Lock = @Lock";

        using (var connection = new SqlConnection(clientSettings.CurrentValue.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    ItemAttempts? result = await connection.QueryFirstOrDefaultAsync<ItemAttempts?>(query, new { Value = value, Lock = taskId }, transaction);
                  
                    await transaction.CommitAsync(cancellationToken);

                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }
    }

    public async Task<bool> DeleteItemAsync(string value, Guid taskId, CancellationToken cancellationToken)
    {
        const string query = "DELETE Items WHERE Value = @Value AND Lock = @Lock";

        using (var connection = new SqlConnection(clientSettings.CurrentValue.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    bool result = (await connection.ExecuteAsync(query, new { Value = value, Lock = taskId }, transaction)) > 0;
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }
    }

    public async Task<bool> DeleteItemAttemptsAsync(string value, CancellationToken cancellationToken)
    {
        const string query = "DELETE ItemAttempts WHERE Value = @Value";

        using (var connection = new SqlConnection(clientSettings.CurrentValue.ConnectionString))
        {
            await connection.OpenAsync(cancellationToken);
            using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    bool result = (await connection.ExecuteAsync(query, new { Value = value }, transaction)) > 0;
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }
    }

    public async Task CleanWorkingData()
    {
        using (var connection = new SqlConnection(clientSettings.CurrentValue.ConnectionString))
        {
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                     await connection.ExecuteAsync("DELETE FROM ItemAttempts", transaction: transaction);
                     await connection.ExecuteAsync("DELETE FROM Items", transaction: transaction);
                     await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
    }
}
