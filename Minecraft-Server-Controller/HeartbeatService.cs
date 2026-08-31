using NLog;
using System.Text.Json;
using NanoDNA.ProcessRunner;

namespace Minecraft_Server_Controller
{
    public class HeartbeatService : BackgroundService
    {
        private ServerSettings Settings { get; set; }

        public MinecraftServerClient Client { get; set; }

        private ServerStatus _Status { get; set; }

        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event Action? OnPingResult;

        public HeartbeatService(ServerStatus status, ServerSettings settings)
        {
            Settings = settings;

            Client = new MinecraftServerClient(Settings);

            _Status = status;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GetResourceUsage(_Status);

                    await Client.PingServer(_Status);

                    _logger.Trace("Server is Live!");

                    _Status.Update();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Minecraft server heartbeat failed.");

                    _Status.Online = false;
                    _Status.Version = "unknown";
                    _Status.OnlinePlayers = 0;
                    _Status.MaxPlayers = 0;
                    _Status.Motd = "unknown";
                    _Status.Latency = TimeSpan.Zero;
                }

                OnPingResult?.Invoke();

                _logger.Trace("Heartbeat cycle complete.");

                await Task.Delay(Settings.Delay);
            }
        }

        private async Task GetResourceUsage(ServerStatus status)
        {
            ProcessRunner runner = new ProcessRunner("docker");

            if (!await runner.TryRunAsync($"stats {Settings.ServerContainerName} --no-stream --format json"))
                return;

            using JsonDocument document = JsonDocument.Parse(runner.STDOutput[0]);

            JsonElement root = document.RootElement;

            if (root.TryGetProperty("CPUPerc", out JsonElement cpuPercent))
                status.CPUPercent = cpuPercent.GetString() ?? "0%";

            if (root.TryGetProperty("MemPerc", out JsonElement memoryPercent))
                status.MemoryPercent = memoryPercent.GetString() ?? "0%";

            if (root.TryGetProperty("MemUsage", out JsonElement memoryUsage))
                status.MemoryUsage = memoryUsage.GetString() ?? "0GiB / 0GiB";
        }
    }
}
