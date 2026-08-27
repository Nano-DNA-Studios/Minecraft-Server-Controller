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

        public ServerStatus Status { get; private set; }

        public ServerSettings Settings { get; private set; }

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public IEnumerable<string> ServerLogs
        {
            get { return LogManager.Configuration?.FindTargetByName<MemoryTarget>("memoryTarget")?.Logs ?? Enumerable.Empty<string>(); }
        }

        public ServerManager(ServerStatus status, ServerSettings settings)
        {
            Status = status;
            Settings = settings;
        }

        public async Task Save()
        {
            _logger.Debug($"Save Requested. Server Online : {Status.Online}");

            if (!Status.Online)
            {
                _logger.Warn("Server Not Online, Cannot Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await Broadcast("Saving...", BroadcastColor.Gold);

            _logger.Trace("Saving Server...");

            await RunCommand("save-all");

            _logger.Info("Saved Server!");

            await Broadcast("Server Saved!", BroadcastColor.Gold);
        }

        public async Task ForceSave()
        {
            _logger.Debug($"Force Save Requested. Server Online : {Status.Online}");

            if (!Status.Online)
            {
                _logger.Warn("Server Not Online, Cannot Force Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await Broadcast("Force Saving, Server May Freeze Momentarily ...", BroadcastColor.Gold);

            await Task.Delay(Settings.Delay);

            _logger.Trace("Force Saving Server...");

            await RunCommand("save-all flush");

            _logger.Info("Finished Force Saving Server");

            await Task.Delay(Settings.Delay);

            await Broadcast("Server Saved!", BroadcastColor.Gold);
        }

        public async Task Stop()
        {
            _logger.Debug($"Stop Requested. Server Online : {Status.Online}");

            if (!Status.Online)
            {
                _logger.Warn("Server Not Online, Cannot Stop Server");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            await ForceSave();

            await Task.Delay(Settings.Delay);

            await Broadcast("Stopping Server...", BroadcastColor.Red);

            _logger.Trace("Stopping Server...");

            await Task.Delay(Settings.Delay);

            await RunCommand("stop");

            await WaitForStop();

            _logger.Info("Finished Stopping Server");
        }

        public async Task Start()
        {
            _logger.Debug($"Start Requested. Server Online : {Status.Online}");

            if (Status.Online)
            {
                _logger.Warn("Server Is Already Online, Cannot Start Server");
                return;
            }

            _logger.Trace("Starting Server...");

            ProcessRunner runner = new ProcessRunner("docker");

            await runner.TryRunAsync($"start {Settings.ServerContainerName}");

            await WaitForStart();

            _logger.Info("Server Started!");
        }

        public async Task Restart()
        {
            _logger.Debug($"Restart Requested. Server Online : {Status.Online}");

            if (!Status.Online)
            {
                _logger.Warn("Server Not Online, Cannot Restart Server");
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
            _logger.Debug($"Backup Requested. Server Online : {Status.Online}");

            if (Status.Online)
            {
                _logger.Warn($"Server is still online during backup, stopping server first...");

                await Task.Delay(Settings.Delay);

                await Stop();
            }

            await Task.Delay(Settings.Delay);

            _logger.Trace("Compressing Server State...");

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
            _logger.Trace($"Broadcast Requested. Server Online : {Status.Online}");

            if (!Status.Online)
            {
                _logger.Warn("Server Not Online, Cannot Broadcast Message");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            _logger.Trace("Broadcasting Message...");

            await RunCommand($"tellraw @a {{\"text\":\"{message}\",\"color\":\"{color.ToString().ToLower()}\"}}");

            _logger.Trace("Broadcast Completed");
        }

        public async Task RunCommand(string command)
        {
            _logger.Trace($"Run Command Requested. Server Online : {Status.Online}");

            if (!Status.Online)
            {
                _logger.Warn("Server Not Online, Cannot Save");
                return;
            }

            RCONCommandRunner runner = new RCONCommandRunner(Settings.RCONHost, Settings.RCONPort, Settings.RCONPassword);

            _logger.Debug($"EXECUTE : {command}");

            await runner.RunAsync(command);

            foreach (string line in runner.OutputLogs)
                _logger.Trace(line);

            foreach (string line in runner.ErrorLogs)
                _logger.Trace(line);
        }

        private async Task WaitForStop()
        {
            _logger.Trace("Waiting for Stop...");

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

            while (Status.Online)
                await Task.Delay(Settings.Delay);

            _logger.Debug("Server has Stopped!");
        }

        private async Task WaitForStart()
        {
            _logger.Trace("Waiting for Start...");

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

            while (!Status.Online)
                await Task.Delay(Settings.Delay);

            _logger.Trace("Server has Started!");
        }

        public string[] GetBackupFiles()
        {
            _logger.Trace("Grabbing Backup Files.");

            if (!Directory.Exists(BACKUP_DIR))
                return new string[0];

            return Directory.GetFiles(BACKUP_DIR, "*.7z");
        }

        public void DeleteBackup(string path)
        {
            if (!File.Exists(path))
            {
                _logger.Warn($"Backup {path} doesn't exist. Skipping Delete");
                return;
            }

            File.Delete(path);

            _logger.Debug($"Deleted backup : {path}");
        }

        public void LoadBackup(string path)
        {
            _logger.Debug($"Loading backup : {path}");

            _logger.Trace($"Deleting current data...");

            foreach (string entry in Directory.GetFileSystemEntries(DATA_DIR))
            {
                if (Directory.Exists(entry))
                    Directory.Delete(entry, true);
                else
                    File.Delete(entry);
            }

            Directory.CreateDirectory(DATA_DIR);

            _logger.Trace($"Finished Deleting Data!");

            _logger.Trace($"Extracting Backup file...");
            ProcessRunner runner = new ProcessRunner("7z");

            runner.Run($"x {path} -o\"/data\" -y");

            _logger.Info($"Finished Extracting Backup!");
        }
    }
}
