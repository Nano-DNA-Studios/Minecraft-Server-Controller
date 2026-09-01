
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
                    _logger.Debug("Server is not online, trying Hourly Service again...");

                    await Task.Delay(10000);
                    continue;
                }

                try
                {
                    await RunHourlyService();
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error occured in Hourly Service : {ex.Message}");
                }
            }
        }

        private async Task RunHourlyService()
        {
            _logger.Trace($"Hourly Save Scheduled. Waiting {GetDelay(true)}.");

            await Task.Delay(GetDelay(true));

            _logger.Info("Running Hourly Save Service...");

            await _Manager.Broadcast("Saving Server in 3 seconds!", BroadcastColor.Red);

            await Task.Delay(Settings.Delay);

            await _Manager.Save();

            _logger.Info("Completed Hourly Save Service!");
        }
    }
}
