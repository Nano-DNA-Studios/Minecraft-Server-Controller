
namespace Minecraft_Server_Controller
{
    public class HourlyService : TimeService
    {
        public HourlyService(ServerManager manager, ServerSettings settings) : base(manager, settings) { } 

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

                await _Manager.Broadcast("Saving Server in 3 seconds!", BroadcastColor.Red);

                await Task.Delay(Settings.Delay);

                await _Manager.Save();
            }
        }
    }
}
