using NanoDNA.AutomationResults;
using NanoDNA.ProcessRunner;

namespace Minecraft_Server_Controller
{
    public class RCONCommandRunner
    {
        private ProcessRunner Runner { get; set; }

        public string Host { get; private set; }

        public int Port { get; private set; }

        public RCONCommandRunner (string host, int port)
        {
            Runner = new ProcessRunner("rcon");

            Host = host;
            Port = port;
        }

        private string BuildRCONArgument(string arg)
        {
            string escapedCommand = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");

            return $"--a server:25575 --p ***REMOVED*** \"{escapedCommand}\"";
        }

        public Result<int> Run (string command)
        {
            string fullCommand = BuildRCONArgument(command);

            return Runner.Run(fullCommand);
        }

        public async Task<Result<int>> RunAsync(string command)
        {
            string fullCommand = BuildRCONArgument(command);

            return await Runner.RunAsync(fullCommand);
        }
    }
}
