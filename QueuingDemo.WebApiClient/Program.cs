using Microsoft.Extensions.Options;
using QueuingDemo.Logic;
using QueuingDemo.Logic.Logging;
using QueuingDemo.Logic.Settings;
using System.Net.Sockets;
using System.Net;
using Polly;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders(); 
builder.Logging.AddProvider(new ColorConsoleLoggerProvider());

builder.Services.Configure<ClientSettings>(builder.Configuration.GetSection("ClientSettings"));

var infiniteRetryPolicy = Policy.HandleResult<HttpResponseMessage>(response => response.RequestMessage != null && response.RequestMessage.Method == HttpMethod.Get &&
                                                                               !response.IsSuccessStatusCode)
                                 .WaitAndRetryForeverAsync(retryAttempt => TimeSpan.FromSeconds(3));

builder.Services.AddHttpClient<ApiService>((serviceProvider, client) =>
{
    var clientSettings = serviceProvider.GetRequiredService<IOptions<ClientSettings>>().Value;
    client.BaseAddress = new Uri(clientSettings.ApiServiceBaseAddress);
}).AddPolicyHandler(infiniteRetryPolicy);


builder.Services.AddSingleton<QueueProcessor>();
builder.Services.AddSingleton<ItemsRepository>();
builder.Services.AddHostedService<HostedService>();
builder.Services.AddControllers();

var app = builder.Build();

SetDynamicUrls(app);

app.MapControllers();

app.Run();


void SetDynamicUrls(WebApplication app)
{
    var listener = new TcpListener(IPAddress.Any, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();

    string url = $"http://localhost:{port}";

    Console.WriteLine($"Dynamically binding to: {url}");

    app.Urls.Add(url);
}


