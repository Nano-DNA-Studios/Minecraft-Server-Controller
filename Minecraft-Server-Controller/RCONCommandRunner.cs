using NanoDNA.AutomationResults;
using NanoDNA.ProcessRunner;
using NLog;

namespace Minecraft_Server_Controller
{
    public class RCONCommandRunner
    {
        private ProcessRunner Runner { get; set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        private string _Password { get; set; }

        public string[] OutputLogs { get; set; }

        public string[] ErrorLogs { get; set; }

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public RCONCommandRunner(string host, int port, string password)
        {
            Runner = new ProcessRunner("rcon");

            Host = host;
            Port = port;
            _Password = password;

            OutputLogs = new string[0];
            ErrorLogs = new string[0];
        }

        private string BuildRCONArgument(string arg)
        {
            string escapedCommand = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");

            return $"--a {Host}:{Port} --p {_Password} \"{escapedCommand}\"";
        }

        public Result<int> Run(string command)
        {
            string fullCommand = BuildRCONArgument(command);

            Result<int> result = Runner.Run(fullCommand);

            OutputLogs = Runner.STDOutput;
            ErrorLogs = Runner.STDError;

            _logger.Trace($"Ran RCON Command : {fullCommand}, Succeeded : {result.IsSuccess}");

            return result;
        }

        public async Task<Result<int>> RunAsync(string command)
        {
            string fullCommand = BuildRCONArgument(command);

            Result<int> result = await Runner.RunAsync(fullCommand);

            OutputLogs = Runner.STDOutput;
            ErrorLogs = Runner.STDError;

            _logger.Trace($"Ran Async RCON Command : {fullCommand}, Succeeded : {result.IsSuccess}");

            return result;
        }
    }
}
