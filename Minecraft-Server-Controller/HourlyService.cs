
namespace Minecraft_Server_Controller
{
    public class HourlyService : BackgroundService
    {
        private ServerStatus _Status { get; set; }

        private RCONCommandRunner _CommandRunner { get; set; }

        public HourlyService(ServerStatus status)
        {
            _Status = status;
            _CommandRunner = new RCONCommandRunner("server", 25575);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(GetDelay());

                try
                {
                    if (!_Status.Online)
                        continue;

                    await _CommandRunner.RunAsync("""tellraw @a {"text":"Saving Server in 5 seconds!", "color":"red"}""");

                    await Task.Delay(5000);

                    await _CommandRunner.RunAsync("""tellraw @a {"text":"Saving...", "color":"gold"}""");
                    await _CommandRunner.RunAsync("save-all");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Something went wrong : {ex.Message}");
                }
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
