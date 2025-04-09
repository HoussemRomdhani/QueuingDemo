using Dapper;
using System.Data.SqlClient;

namespace QueuingDemo.ExternalApi;

public class ItemRepository
{
    private readonly string connectionString;
    public ItemRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<List<Item>> GetAllAsync()
    {
        const string query = "SELECT Id, Value FROM Reference";
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            return (await connection.QueryAsync<Item>(query)).AsList();
        }
    }

    public async Task<Item?> Get()
    {
        const string query = "SELECT TOP 1 Id, Value FROM Reference ORDER BY 1";
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<Item>(query);
        }
    }

    public async Task CreateAsync(string item)
    {
        const string query = "INSERT INTO Reference (Value) OUTPUT INSERTED.Id VALUES (@Value)";

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync(query, new { Value = item });
        }
    }

    public async Task DeleteAsync(string value)
    {
        const string query = "DELETE FROM Reference WHERE Value = @Value";
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync(query, new { Value = value });
        }
    }

    // Read by Id
    public async Task<Item?> GetByIdAsync(int id)
    {
        const string query = "SELECT Id, Value FROM Items WHERE Id = @Id";

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Query the database and return a single item
        return await connection.QuerySingleOrDefaultAsync<Item>(query, new { Id = id });
    }

   

    // Update
    public async Task<bool> UpdateAsync(Item item)
    {
        const string query = "UPDATE Items SET Value = @Value WHERE Id = @Id";

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // Execute the query and return if any rows were affected
        var rowsAffected = await connection.ExecuteAsync(query, new { item.Value, item.Id });
        return rowsAffected > 0;
    }

    
}


