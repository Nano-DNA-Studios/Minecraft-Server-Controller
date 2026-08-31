using Minecraft_Server_Controller.Components;
using NLog.Web;
using NLog;

namespace Minecraft_Server_Controller
{
    public class Program
    {
        private static int HTTPPort = 80;
        private static int HTTPSPort = 443;

        public static void Main(string[] args)
        {
            Logger logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

            var builder = WebApplication.CreateBuilder(args);

            if (!OperatingSystem.IsWindows())
            {
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(HTTPPort); // HTTP
                    //options.ListenAnyIP(HTTPSPort, listenOptions =>
                    //{
                    //    listenOptions.UseHttps(certPath, certPassword);
                    //});
                });
            }

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            ServerSettings settings = new ServerSettings();
            ServerStatus status = new ServerStatus();
            HeartbeatService heartbeat = new HeartbeatService(status, settings);
            ServerManager manager = new ServerManager(status, settings);
            HourlyService hourly = new HourlyService(manager, settings);
            DailyService daily = new DailyService(manager, settings);

            builder.Services.AddSingleton<ServerSettings>(settings);
            builder.Services.AddSingleton<ServerStatus>(status);
            builder.Services.AddSingleton<DailyService>(daily);
            builder.Services.AddSingleton<ServerManager>(manager);
            builder.Services.AddSingleton<HeartbeatService>(heartbeat);
            builder.Services.AddSingleton<HourlyService>(hourly);

            builder.Services.AddHostedService<HeartbeatService>(serviceProvider => heartbeat);
            builder.Services.AddHostedService<HourlyService>(serviceProvider => hourly);
            builder.Services.AddHostedService<DailyService>(serviceProvider => daily);

            builder.Services.AddHttpClient("Map", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(2);
            });

            builder.Logging.AddFilter("System.Net.Http.HttpClient.Map.LogicalHandler", Microsoft.Extensions.Logging.LogLevel.None);
            builder.Logging.AddFilter("System.Net.Http.HttpClient.Map.ClientHandler", Microsoft.Extensions.Logging.LogLevel.None);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            try
            {
                app.Run();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}
