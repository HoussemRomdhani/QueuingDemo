namespace QueuingDemo.ExternalApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddSingleton(new ItemRepository(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddHostedService<TimedHostedService>();

        var app = builder.Build();

        app.MapControllers();

        app.Run();
    }
}
