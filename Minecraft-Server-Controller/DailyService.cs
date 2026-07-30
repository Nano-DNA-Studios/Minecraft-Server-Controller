
using NanoDNA.ProcessRunner;

namespace Minecraft_Server_Controller
{
    public class DailyService : BackgroundService
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Starting Daily Service");
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("Running RCON Command");
                try
                {
                    ProcessRunner runner = new("rcon");

                    await runner.TryRunAsync("--a server:25575 --p ***REMOVED*** \"say Server restarting in 5 minutes.\"");

                    await Task.Delay(2000);
                    
                    await runner.TryRunAsync("--a server:25575 --p ***REMOVED*** \"say Saving...\"");

                    bool result = await runner.TryRunAsync("--a server:25575 --p ***REMOVED*** \"save-all\"");

                    if (result)
                    {
                        foreach (string line in runner.STDOutput)
                            Console.WriteLine(line);

                    } else
                    {
                        Console.WriteLine("Failure Occured");

                        foreach (string line in runner.STDError)
                            Console.WriteLine(line);
                    }

                    await Task.Delay(5000);

                    bool result2 = await runner.TryRunAsync("--a server:25575 --p ***REMOVED*** \"dynmap fullrender world\"");

                }
                catch (Exception e)
                {
                    Console.WriteLine($"Something went wrong : {e.Message}");
                }

                await Task.Delay(30000);
            }
        }
    }
}
