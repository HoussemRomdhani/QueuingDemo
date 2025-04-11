namespace QueuingDemo.ExternalApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
        builder.Services.AddSingleton<ItemsRepository>();
        builder.Services.AddHostedService<HostedService>();

        var app = builder.Build();

        app.MapControllers();

        app.Run();
    }
}
