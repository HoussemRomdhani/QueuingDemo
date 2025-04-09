using System.Text.Json;

namespace QueuingDemo.Logic;

public class ApiService
{
    private readonly HttpClient _httpClient;
    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
 
    public async Task<string?> GetItemAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(_httpClient.BaseAddress, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

         return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task DeleteItemAsync(string value, CancellationToken cancellationToken = default)
    {
       await _httpClient.DeleteAsync($"{_httpClient.BaseAddress}/{value}");
    }

    public async Task<IList<string>> GetItemsAsync()
    {
        var response = await _httpClient.GetAsync("");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var items = JsonSerializer.Deserialize<List<string>>(json);

        return items ?? new List<string>();
    }

}
