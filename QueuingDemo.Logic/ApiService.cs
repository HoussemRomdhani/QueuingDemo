using Microsoft.Extensions.Logging;

namespace QueuingDemo.Logic;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiService> logger;
    public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        this.logger = logger;
    }
 
    public async Task<string?> GetItemAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(_httpClient.BaseAddress, cancellationToken);

            return response.StatusCode == System.Net.HttpStatusCode.OK ? await response.Content.ReadAsStringAsync(cancellationToken) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "⚠️ {message}", ex.Message);
        }

        return null;
    }

    public async Task<bool> DeleteItemAsync(string value, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync($"{_httpClient.BaseAddress}/{value}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "⚠️ {message}", ex.Message);
        }

        return false;
    }
}
