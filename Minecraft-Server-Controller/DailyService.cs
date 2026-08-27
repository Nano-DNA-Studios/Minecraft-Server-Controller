
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
                    _logger.Debug("Server is not online, trying Daily Service again...");

                    await Task.Delay(10000);
                    continue;
                }

                _logger.Trace($"Daily Backup Scheduled. Waiting {GetDelay(false)}.");

                await Task.Delay(GetDelay(false));

                _logger.Info("Running Daily Restart & Backup Service...");

                await _Manager.Broadcast("Restarting Server for Backup...", BroadcastColor.Red);

                await Task.Delay(Settings.Delay);

                await _Manager.Backup();

                await Task.Delay(Settings.Delay);

                await _Manager.Start();

                _logger.Info("Completed Daily Restart & Backup Service!");
            }
        }
    }
}
