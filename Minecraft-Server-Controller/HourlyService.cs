
namespace Minecraft_Server_Controller
{
    public class HourlyService : BackgroundService
    {
        private ServerManager _Manager { get; set; }

        public HourlyService(ServerManager serverManager)
        {
            _Manager = serverManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_Manager.Status.Online)
                {
                    await Task.Delay(10000);
                    continue;
                }

                await Task.Delay(GetDelay());

                await _Manager.Broadcast("Saving Server in 5 seconds!", BroadcastColor.Red);

                await Task.Delay(5000);

                await _Manager.Save();
            }
        }

        private TimeSpan GetDelay()
        {
            DateTime now = DateTime.Now;
            DateTime nextHour = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute, // Set this to 0 and change to AddHour(1);
                0,
                now.Kind).AddMinutes(5);

            TimeSpan delay = nextHour - now;

            return delay;
        }
    }
}
