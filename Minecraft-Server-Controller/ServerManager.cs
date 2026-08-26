using NanoDNA.ProcessRunner;

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

        public ServerSettings Settings { get; private set; }

        public const string BACKUP_DIR = "/backup";

        public const string DATA_DIR = "/data";

        public ServerManager(ServerStatus status, ServerSettings settings)
        {
            Status = status;
            Settings = settings;
            ServerLogs = new List<string>();
        }

        public async Task Save()
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await Broadcast("Saving...", BroadcastColor.Gold);

            AddLog(LogLevel.Log, "Saving Server...");

            await RunCommand("save-all");

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

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await Broadcast("Force Saving, Server May Freeze Momentarily ...", BroadcastColor.Gold);

            await Task.Delay(Settings.Delay);

            AddLog(LogLevel.Log, "Force Saving Server...");

            await RunCommand("save-all flush");

            AddLog(LogLevel.Log, "Finished Force Saving Server");

            await Task.Delay(Settings.Delay);

            await Broadcast("Server Saved!", BroadcastColor.Gold);
        }

        public async Task Stop()
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Stop Server");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await ForceSave();

            await Task.Delay(Settings.Delay);

            await Broadcast("Stopping Server...", BroadcastColor.Red);

            AddLog(LogLevel.Log, "Stopping Server...");

            await Task.Delay(Settings.Delay);

            await RunCommand("stop");

            await WaitForStop();

            AddLog(LogLevel.Log, "Finished Stopping Server...");
        }

        public async Task Start()
        {
            if (Status.Online)
            {
                AddLog(LogLevel.Error, "Server Is Already Online, Cannot Start Server");
                return;
            }

            AddLog(LogLevel.Log, "Starting Server...");

            ProcessRunner runner = new ProcessRunner("docker");

            await runner.TryRunAsync($"start {Settings.ServerContainerName}");

            await WaitForStart();

            while (!Status.Online)
                await Task.Delay(Settings.Delay);

            AddLog(LogLevel.Log, "Server Started!");
        }

        public async Task Restart()
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Restart Server");
                return;
            }

            await Broadcast("Restarting Server...", BroadcastColor.Red);

            AddLog(LogLevel.Log, "Restarting Server...");

            await Stop();

            await Task.Delay(Settings.Delay);

            await Start();

            AddLog(LogLevel.Log, "Server Restarted!");
        }

        public async Task Backup()
        {
            AddLog(LogLevel.Log, "Backing Up Server...");

            if (Status.Online)
            {
                await Task.Delay(Settings.Delay);

                await Stop();
            }

            await Task.Delay(Settings.Delay);

            AddLog(LogLevel.Log, "Compressing Server State...");

            DateTime now = DateTime.Now;

            string backupPath = $"/backup/Backup-{now:yyyy-MM-dd-HH-mm-ss}.7z";
            string command = $"cd /data && exec 7z a -mx=9 {backupPath} .";

            CommandRunner runner = new CommandRunner(application: NanoDNA.ProcessRunner.Enums.ProcessApplication.Sh);

            AddLog(LogLevel.Execute, command);

            await runner.RunAsync(command);

            await Task.Delay(Settings.Delay);

            AddLog(LogLevel.Log, "Server Compressed. Ready to Start Server");

            string[] files = GetBackupFiles();

            if (files.Length > Settings.NumOfBackups)
                DeleteBackup(files[0]);
        }

        public async Task Broadcast(string message, BroadcastColor color)
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Broadcast Message");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            AddLog(LogLevel.Log, "Broadcasting Message...");

            await RunCommand($"tellraw @a {{\"text\":\"{message}\",\"color\":\"{color.ToString().ToLower()}\"}}");

            AddLog(LogLevel.Log, "Broadcast Completed");
        }

        public void AddLog(LogLevel level, string message)
        {
            DateTime now = DateTime.Now;

            string log = $"[{now:yyyy-MM-dd HH:mm:ss}] [{GetLogLevel(level)}] : {message}";

            Console.WriteLine(log);

            ServerLogs.Insert(0, log);
        }

        public async Task RunCommand(string command)
        {
            if (!Status.Online)
            {
                AddLog(LogLevel.Error, "Server Not Online, Cannot Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

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

        private async Task WaitForStop()
        {
            ProcessRunner runner = new ProcessRunner("docker");

            string running = "true";

            while (running != "false")
            {
                await Task.Delay(Settings.Delay);

                bool result = await runner.TryRunAsync(string.Join(" ", "inspect -f {{.State.Running}}", Settings.ServerContainerName));

                if (result)
                    running = runner.STDOutput.Last();
            }
        }

        private async Task WaitForStart()
        {
            ProcessRunner runner = new ProcessRunner("docker");

            string running = "false";

            while (running != "true")
            {
                await Task.Delay(Settings.Delay);

                bool result = await runner.TryRunAsync(string.Join(" ", "inspect -f {{.State.Running}}", Settings.ServerContainerName));

                if (result)
                    running = runner.STDOutput.Last();
            }
        }

        public string[] GetBackupFiles()
        {
            if (!Directory.Exists(BACKUP_DIR))
                return new string[0];

            return Directory.GetFiles(BACKUP_DIR);
        }

        public void DeleteBackup(string path)
        {
            if (!File.Exists(path))
                return;

            File.Delete(path);

            AddLog(LogLevel.Log, $"Deleted backup : {path}");
        }

        public void LoadBackup(string path)
        {
            AddLog(LogLevel.Log, $"Loading backup : {path}");

            AddLog(LogLevel.Log, $"Deleting current data...");

            foreach (string entry in Directory.GetFileSystemEntries(DATA_DIR))
            {
                if (Directory.Exists(entry))
                    Directory.Delete(entry, true);
                else
                    File.Delete(entry);
            }

            Directory.CreateDirectory(DATA_DIR);

            AddLog(LogLevel.Log, $"Finished Deleting Data!");

            AddLog(LogLevel.Log, $"Extracting Backup file...");

            ProcessRunner runner = new ProcessRunner("7z");

            runner.Run($"x {path} -o\"/data\" -y");

            AddLog(LogLevel.Log, $"Finished Extracting Backup!");
        }
    }
}
