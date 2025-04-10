using QueuingDemo.Logic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<ApiService>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5165/api");
});

builder.Services.AddSingleton<QueueProcessor>();
builder.Services.AddSingleton<ItemsRepository>();
builder.Services.AddHostedService<HostedService>();
builder.Services.AddControllers();
var app = builder.Build();


app.MapControllers();

app.Run();
