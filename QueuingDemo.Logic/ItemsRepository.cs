using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.SqlClient;
namespace QueuingDemo.Logic;


public class ItemsRepository
{
    private readonly string connectionString;
    private readonly ILogger<ItemsRepository> logger;
    public ItemsRepository(IConfiguration configuration, ILogger<ItemsRepository> logger)
    {
        connectionString = configuration.GetConnectionString("DefaultConnection")!;
        this.logger = logger;
    }

    public async Task<Item?> GetItemForProcess(string value, Guid taskId, CancellationToken cancellationToken = default)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync(default);
            using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                try
                {
                    string query = @"SELECT TOP 1 * FROM Items WHERE Value = @Value AND Lock = @Lock";
                    
                    Item? result = await connection.QueryFirstOrDefaultAsync<Item>(query, new { Value = value, Lock = taskId }, transaction);

                    await transaction.CommitAsync();

                    return result;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }
    }

    public async Task<Result> ProcessItemAsync(string value, Guid taskId,
                                       Func<string, CancellationToken, Task<bool>> processor,
                                       Func<string, CancellationToken, Task<bool>> callBack,
                                       int maxAttempts = 5,
                                       CancellationToken cancellationToken = default)
    {
        Result done = Result.Fail;
        bool success = await processor(value, cancellationToken);

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync(default);
            using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                try
                {
                    logger.LogInformation("SELECT TOP 1 * FROM Items WHERE Value = {Value} AND Lock = {lock}", value, taskId);
                 
                    Item? item = await connection.QueryFirstOrDefaultAsync<Item?>("SELECT TOP 1 * FROM Items WHERE Value = @Value AND Lock = @Lock", new { Value = value, Lock = taskId }, transaction);

                    if (item !=  null)
                    {
                        logger.LogInformation("[ROW] SELECT TOP 1 * FROM Items WHERE Value = {Value} AND Lock = {lock} ", value, taskId);
                    }
                    else
                    {
                        logger.LogInformation("[NOPE] SELECT TOP 1 * FROM Items WHERE Value = {Value} AND Lock = {lock}", value, taskId);
                    }

                    if (success)
                    {
                        if (item != null)
                        {
                            bool deleted = (await connection.ExecuteAsync("DELETE FROM Items WHERE Value = @Value AND Lock = @Lock", new { item.Value, Lock = taskId }, transaction)) > 0;
                            if (deleted)
                            {
                                 done = Result.Success;
                            }
                        }

                        done = Result.Success;
                    }
                    else
                    {
                        if (item != null)
                        {
                            if (item.Attempts < maxAttempts)
                            {
                                string query = @"UPDATE Items SET Attempts = Attempts + 1 WHERE Value = @Value AND Attempts < @Attempts AND Lock = @Lock";
                                await connection.ExecuteAsync(query, new { Value = value, Attempts = maxAttempts, Lock  = taskId }, transaction);
                            }
                            else
                            {
                                bool deleted = (await connection.ExecuteAsync("DELETE Items  WHERE Value = @Value AND Lock = @Lock", new { item.Value, Lock = taskId }, transaction)) > 0;
                                if (deleted)
                                {
                                    done = Result.Success;
                                }
                            }
                        }
                        else
                        {
                            string query = @" IF NOT EXISTS (SELECT 1 FROM Items WHERE Value = @Value)
                                                  BEGIN
                                                        INSERT INTO Items (Value, Attempts, Lock) VALUES (@Value, @Attempts, @Lock)
                                                  END";
                            await connection.ExecuteAsync(query, new { Value = value, Attempts = 1, Lock = taskId }, transaction);
                        }
                    }

                    await transaction.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }

        if (done == Result.Success)
        {
            bool result = await callBack(value, cancellationToken);
         
            if (result)
            {
                logger.LogInformation("Item was deleted {item}: ", value);
            }
            else
            {
            }
        }

        return done;
    }
}
