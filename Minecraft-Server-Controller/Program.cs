using Minecraft_Server_Controller.Components;

namespace Minecraft_Server_Controller
{
    public class Program
    {
        private static int HTTPPort = 80;
        private static int HTTPSPort = 443;

        public static void Main(string[] args)
        {
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

            ServerStatus status = new ServerStatus();
            HeartbeatService heartbeat = new HeartbeatService(status);
            ServerManager manager = new ServerManager(status);
            HourlyService hourly = new HourlyService(manager);
            DailyService daily = new DailyService(manager);

            builder.Services.AddSingleton<ServerStatus>(status);
            builder.Services.AddSingleton<DailyService>(daily);
            builder.Services.AddSingleton<ServerManager>(manager);
            builder.Services.AddSingleton<HeartbeatService>(heartbeat);
            builder.Services.AddSingleton(hourly);

            builder.Services.AddHostedService<HeartbeatService>(serviceProvider => heartbeat);
            builder.Services.AddHostedService<HourlyService>(serviceProvider => hourly);
            builder.Services.AddHostedService<DailyService>(serviceProvider => daily);

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

            app.Run();
        }
    }
}
