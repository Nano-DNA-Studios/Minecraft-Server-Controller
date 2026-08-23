
namespace Minecraft_Server_Controller
{
    public class HourlyService : TimeService
    {
        public HourlyService(ServerManager serverManager) : base(serverManager) { } 

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

                await _Manager.Broadcast("Saving Server in 5 seconds!", BroadcastColor.Red);

                await Task.Delay(5000);

                await _Manager.Save();
            }
        }
    }
}
