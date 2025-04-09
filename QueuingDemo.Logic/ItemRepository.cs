using Dapper;
using Microsoft.VisualBasic;
using System.Data;
using System.Data.SqlClient;
namespace QueuingDemo.Logic;

public class ItemRepository
{
    private readonly string connectionString;
    public ItemRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task TryDeleteAsync(string value, CancellationToken cancellationToken = default)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    string selectQuery = "SELECT TOP 1  * FROM Item WHERE Value = @Value";
                    Item? item = await connection.QueryFirstOrDefaultAsync<Item>(selectQuery, new { Value = value }, transaction);

                    if (item != null)
                    {
                        const string query = "DELETE FROM Item WHERE Value  = @Value AND RowVersion = @RowVersion";
                        await connection.ExecuteAsync(query, new { item.Value, item.RowVersion }, transaction);
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }
    }

    public async Task<bool> ProcessItemAsync(string value, 
                                       Func<string, CancellationToken, Task<bool>> processor,
                                       Func<string, CancellationToken, Task> callBack,
                                       int maxAttempts = 5,
                                       CancellationToken cancellationToken = default)
    {
        bool done = false;

        bool success = await processor(value, cancellationToken);

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync(default);
            using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                try
                {
                    await connection.ExecuteAsync("SET LOCK_TIMEOUT 5000;", transaction: transaction);
                    string selectQuery =
                      @"SELECT TOP 1  * FROM Item 
                             WITH (ROWLOCK, UPDLOCK)
                             WHERE Value = @Value";

                    Item? item = await connection.QueryFirstOrDefaultAsync<Item>(selectQuery, new { Value = value }, transaction);

                    if (success)
                    {
                        if (item != null)
                        {
                            await connection.ExecuteAsync("DELETE FROM Item WHERE Value = @Value", new { item.Value }, transaction);
                            await callBack(item.Value, cancellationToken);
                        }

                        done = true;
                    }
                    else
                    {
                        if (item != null)
                        {
                            if (item.Attempts < maxAttempts)
                            {
                                string query = @"UPDATE Item SET Attempts = Attempts + 1 WHERE Value = @Value AND Attempts < @Attempts";
                                await connection.ExecuteAsync(query, new { Value = value, Attempts = maxAttempts }, transaction);
                            }
                            else
                            {
                                await connection.ExecuteAsync("DELETE FROM Item WHERE Value = @Value", new { item.Value }, transaction);
                                await callBack(item.Value, cancellationToken);
                                done = true;
                            }
                        }
                        else
                        {
                             string query = @"INSERT INTO Item (Value, Attempts) VALUES (@Value, @Attempts)";
                             await connection.ExecuteAsync(query, new { Value = value, Attempts = 1 }, transaction);
                        }
                    }

                    await transaction.CommitAsync(cancellationToken);

                    return done;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            }
        }
    }
}
