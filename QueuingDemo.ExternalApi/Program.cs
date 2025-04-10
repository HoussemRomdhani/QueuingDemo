namespace QueuingDemo.ExternalApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddSingleton<ItemsRepository>();
        builder.Services.AddHostedService<TimedHostedService>();

        var app = builder.Build();

        app.MapControllers();

        app.Run();
    }
}
