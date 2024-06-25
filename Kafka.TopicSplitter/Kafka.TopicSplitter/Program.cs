using Serilog;
using Serilog.Events;

namespace Kafka.TopicSplitter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((hostContext, services) =>
                {
                    hostContext.HostingEnvironment.ApplicationName = nameof(Program);

                    services.Configure<HostOptions>(option =>
                    {
                        option.ShutdownTimeout = TimeSpan.FromSeconds(10);
                    });
                })
                .UseSerilog((_, log) =>
                {
                    log
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                        .MinimumLevel.Override("MassTransit.EntityFrameworkCoreIntegration", LogEventLevel.Warning)
                        .Enrich.FromLogContext()
                        .WriteTo.Console();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseWindowsService();
    }
}