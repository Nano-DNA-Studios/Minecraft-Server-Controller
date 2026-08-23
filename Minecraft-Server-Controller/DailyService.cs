
namespace Minecraft_Server_Controller
{
    public class DailyService : TimeService
    {
        public DailyService(ServerManager manager) : base(manager) { }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_Manager.Status.Online)
                {
                    await Task.Delay(10000);
                    continue;
                }

                await Task.Delay(GetDelay(true));

                await _Manager.Broadcast("Restarting Server for Backup...", BroadcastColor.Red);

                await Task.Delay(5000);

                await _Manager.Stop();

                await Task.Delay(5000);

                await _Manager.Backup();

                await Task.Delay(5000);

                await _Manager.Start();
            }
        }
    }
}
