
namespace Minecraft_Server_Controller
{
    public class HeartbeatService : BackgroundService
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }

        private ServerSettings Settings { get; set; }

        public MinecraftServerClient Client { get; set; }

        private ServerStatus _Status { get; set; }

        public event Action? OnPingResult;

        public HeartbeatService(ServerStatus status, ServerSettings settings)
        {
            Settings = settings;

            CancellationTokenSource = new CancellationTokenSource();

            Client = new MinecraftServerClient(Settings);

            _Status = status;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Starting Service");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Client.PingServer(_Status);

                    _Status.Update();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Something went wrong with ping! {e.Message}");

                    _Status.Online = false;
                    _Status.Version = "unknown";
                    _Status.OnlinePlayers = 0;
                    _Status.MaxPlayers = 0;
                    _Status.Motd = "unknown";
                    _Status.Latency = TimeSpan.Zero;
                }

                OnPingResult?.Invoke();

                await Task.Delay(Settings.Delay);
            }
        }
    }
}
