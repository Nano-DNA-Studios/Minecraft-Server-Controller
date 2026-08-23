
namespace Minecraft_Server_Controller
{
    public class DailyService : TimeService
    {
        public DailyService(ServerManager manager, ServerSettings settings) : base(manager, settings) { }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_Manager.Status.Online)
                {
                    await Task.Delay(10000);
                    continue;
                }

                await Task.Delay(GetDelay(false));

                await _Manager.Broadcast("Restarting Server for Backup...", BroadcastColor.Red);

                await Task.Delay(Settings.Delay);

                await _Manager.Backup();

                await Task.Delay(Settings.Delay);

                await _Manager.Start();
            }
        }
    }
}
