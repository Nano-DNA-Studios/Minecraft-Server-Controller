
namespace Minecraft_Server_Controller
{
    public enum LogLevel
    {
        Log,
        Error,
        Execute
    }

    public enum BroadcastColor
    {
        Gold,
        Blue,
        Red,
        Green
    }


    public class ServerManager
    {
        public List<string> ServerLogs { get; private set; }

        public ServerStatus Status { get; private set; }

        public ServerManager(ServerStatus status)
        {
            Status = status;
            ServerLogs = new List<string>();
        }

        public async Task Save()
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner("server", 25575);

            await Broadcast("Saving...", BroadcastColor.Gold);

            string command = "save-all";

            AddLog(LogLevel.Log, "Saving Server...");

            await RunCommand(command);

            AddLog(LogLevel.Log, "Finished Saving Server");

            await Broadcast("Server Saved!", BroadcastColor.Gold);
        }

        public async Task ForceSave()
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Force Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner("server", 25575);

            await Broadcast("Force Saving, Server May Freeze Momentarily ...", BroadcastColor.Gold);

            string command = "save-all flush";

            AddLog(LogLevel.Log, "Force Saving Server...");

            await RunCommand(command);

            AddLog(LogLevel.Log, "Finished Force Saving Server");

            await Broadcast("Server Saved!", BroadcastColor.Gold);
        }

        public async Task Broadcast(string message, BroadcastColor color)
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Broadcast Message");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner("server", 25575);

            string command = $"tellraw @a {{\"text\":\"{message}\",\"color\":\"{color}\"}}";

            AddLog(LogLevel.Log, "Broadcasting Message...");

            await RunCommand(command);

            AddLog(LogLevel.Log, "Broadcast Completed");
        }

        public void AddLog(LogLevel level, string message)
        {
            DateTime now = DateTime.Now;

            ServerLogs.Insert(0, $"[{now:yyyy-MM-dd HH:mm:ss}] [{GetLogLevel(level)}] : {message}");
        }

        public async Task RunCommand(string command)
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner("server", 25575);

            AddLog(LogLevel.Execute, command);

            await runner.RunAsync(command);

            foreach (string line in runner.OutputLogs)
                AddLog(LogLevel.Log, line);

            foreach (string line in runner.ErrorLogs)
                AddLog(LogLevel.Error, line);
        }

        private string GetLogLevel(LogLevel level)
        {
            string logLevel = string.Empty;

            if (level == LogLevel.Log)
                logLevel = "LOGS";
            else if (level == LogLevel.Error)
                logLevel = "ERROR";
            else if (level == LogLevel.Execute)
                logLevel = "EXECUTE";

            return logLevel;
        }

        private string GetColorStr(BroadcastColor color)
        {
            return color.ToString().ToLower();
        }



    }
}
