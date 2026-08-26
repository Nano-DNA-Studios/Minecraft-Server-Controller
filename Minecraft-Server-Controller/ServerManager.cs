using NLog;
using NLog.Targets;
using NanoDNA.ProcessRunner;

namespace Minecraft_Server_Controller
{
    public enum BroadcastColor
    {
        Gold,
        Blue,
        Red,
        Green
    }

    public class ServerManager
    {
        public const string BACKUP_DIR = "/backup";

        public const string DATA_DIR = "/data";

        public IEnumerable<string> ServerLogs
        {
            get
            {
                var target = LogManager.Configuration?.FindTargetByName<MemoryTarget>("memoryTarget");
                return target?.Logs.Reverse() ?? Enumerable.Empty<string>();
            }
        }

        public ServerStatus Status { get; private set; }

        public ServerSettings Settings { get; private set; }

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ServerManager(ServerStatus status, ServerSettings settings)
        {
            Status = status;
            Settings = settings;
        }

        public async Task Save()
        {
            if (!Status.Online)
            {
                _logger.Error("Server Not Online, Cannot Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await Broadcast("Saving...", BroadcastColor.Gold);

            _logger.Info("Saving Server...");

            await RunCommand("save-all");

            _logger.Info("Finished Saving Server");

            await Broadcast("Server Saved!", BroadcastColor.Gold);
        }

        public async Task ForceSave()
        {
            if (!Status.Online)
            {
                _logger.Error("Server Not Online, Cannot Force Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await Broadcast("Force Saving, Server May Freeze Momentarily ...", BroadcastColor.Gold);

            await Task.Delay(Settings.Delay);

            _logger.Info("Force Saving Server...");

            await RunCommand("save-all flush");

            _logger.Info("Finished Force Saving Server");

            await Task.Delay(Settings.Delay);

            await Broadcast("Server Saved!", BroadcastColor.Gold);
        }

        public async Task Stop()
        {
            if (!Status.Online)
            {
                _logger.Error("Server Not Online, Cannot Stop Server");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await ForceSave();

            await Task.Delay(Settings.Delay);

            await Broadcast("Stopping Server...", BroadcastColor.Red);

            _logger.Info("Stopping Server...");

            await Task.Delay(Settings.Delay);

            await RunCommand("stop");

            await WaitForStop();

            _logger.Info("Finished Stopping Server");
        }

        public async Task Start()
        {
            if (Status.Online)
            {
                _logger.Error("Server Is Already Online, Cannot Start Server");
                return;
            }

            _logger.Info("Starting Server...");

            ProcessRunner runner = new ProcessRunner("docker");

            await runner.TryRunAsync($"start {Settings.ServerContainerName}");

            await WaitForStart();

            while (!Status.Online)
                await Task.Delay(Settings.Delay);

            _logger.Info("Server Started!");
        }

        public async Task Restart()
        {
            if (!Status.Online)
            {
                _logger.Error("Server Not Online, Cannot Restart Server");
                return;
            }

            await Broadcast("Restarting Server...", BroadcastColor.Red);

            _logger.Info("Restarting Server...");

            await Stop();

            await Task.Delay(Settings.Delay);

            await Start();

            _logger.Info("Server Restarted!");
        }

        public async Task Backup()
        {
            _logger.Info("Backing Up Server...");

            if (Status.Online)
            {
                await Task.Delay(Settings.Delay);

                await Stop();
            }

            await Task.Delay(Settings.Delay);

            _logger.Info("Compressing Server State...");

            DateTime now = DateTime.Now;

            string backupPath = $"/backup/Backup-{now:yyyy-MM-dd-HH-mm-ss}.7z";
            string command = $"cd /data && exec 7z a -mx=9 {backupPath} .";

            CommandRunner runner = new CommandRunner(application: NanoDNA.ProcessRunner.Enums.ProcessApplication.Sh);

            _logger.Debug($"EXECUTE : {command}");

            await runner.RunAsync(command);

            await Task.Delay(Settings.Delay);

            _logger.Info("Server Compressed. Ready to Start Server");

            string[] files = GetBackupFiles();

            if (files.Length > Settings.NumOfBackups)
                DeleteBackup(files[0]);
        }

        public async Task Broadcast(string message, BroadcastColor color)
        {
            if (!Status.Online)
            {
                _logger.Error("Server Not Online, Cannot Broadcast Message");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            _logger.Info("Broadcasting Message...");

            await RunCommand($"tellraw @a {{\"text\":\"{message}\",\"color\":\"{color.ToString().ToLower()}\"}}");

            _logger.Info("Broadcast Completed");
        }

        public async Task RunCommand(string command)
        {
            if (!Status.Online)
            {
                _logger.Error("Server Not Online, Cannot Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            _logger.Debug($"EXECUTE : {command}");

            await runner.RunAsync(command);

            foreach (string line in runner.OutputLogs)
                _logger.Info(line);

            foreach (string line in runner.ErrorLogs)
                _logger.Error(line);
        }

        private async Task WaitForStop()
        {
            _logger.Info("Waiting for Stop...");

            ProcessRunner runner = new ProcessRunner("docker");

            string running = "true";

            while (running != "false")
            {
                await Task.Delay(Settings.Delay);

                bool result = await runner.TryRunAsync(string.Join(" ", "inspect -f {{.State.Running}}", Settings.ServerContainerName));

                if (result)
                    running = runner.STDOutput.Last();

                _logger.Trace($"Docker Inspect : {running}");
            }
        }

        private async Task WaitForStart()
        {
            _logger.Info("Waiting for Start...");

            ProcessRunner runner = new ProcessRunner("docker");

            string running = "false";

            while (running != "true")
            {
                await Task.Delay(Settings.Delay);

                bool result = await runner.TryRunAsync(string.Join(" ", "inspect -f {{.State.Running}}", Settings.ServerContainerName));

                if (result)
                    running = runner.STDOutput.Last();

                _logger.Trace($"Docker Inspect : {running}");
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

            _logger.Info($"Deleted backup : {path}");
        }

        public void LoadBackup(string path)
        {
            _logger.Info($"Loading backup : {path}");

            _logger.Info($"Deleting current data...");

            foreach (string entry in Directory.GetFileSystemEntries(DATA_DIR))
            {
                if (Directory.Exists(entry))
                    Directory.Delete(entry, true);
                else
                    File.Delete(entry);
            }

            Directory.CreateDirectory(DATA_DIR);

            _logger.Info($"Finished Deleting Data!");

            _logger.Info($"Extracting Backup file...");
            ProcessRunner runner = new ProcessRunner("7z");

            runner.Run($"x {path} -o\"/data\" -y");

            _logger.Info($"Finished Extracting Backup!");
        }
    }
}
