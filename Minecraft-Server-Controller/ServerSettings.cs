using NLog;

namespace Minecraft_Server_Controller
{
    public class ServerSettings
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public string RCONHost { get; private set; }

        public int RCONPort { get; private set; }

        public string RCONPassword { get; private set; }

        public int Delay { get; private set; }

        public int NumOfBackups { get; private set; }

        public string ServerName { get; private set; }

        public int MapPort { get; private set; }

        public int ServerPort { get; private set; }

        public int ControllerPort { get; private set; }

        public string MapHealthUrl { get; private set; }

        public string MapBrowserUrl { get; private set; }

        public string ServerContainerName { get; private set; }

        public ServerSettings()
        {
            RCONHost = string.Empty;
            RCONPort = 25575;
            RCONPassword = string.Empty;
            ServerName = string.Empty;
            Delay = 3000;
            NumOfBackups = 3;
            MapPort = 8123;
            ServerPort = 25565;
            ControllerPort = 8080;
            MapBrowserUrl = string.Empty;
            MapHealthUrl = string.Empty;
            ServerContainerName = string.Empty;

            LoadEnvFromFile();
            LoadEnv();
        }

        private bool LoadInt(string varName, out int value)
        {
            string? strVal = Environment.GetEnvironmentVariable(varName);

            if (string.IsNullOrEmpty(strVal))
            {
                value = 0;
                return false;
            }

            if (!int.TryParse(strVal, out int intVal))
            {
                value = 0;
                return false;
            }

            value = intVal;
            _logger.Debug($"Loaded Variable from Environment : {varName}={value}");
            return true;
        }

        private bool LoadStr(string varName, out string value)
        {
            string? strVal = Environment.GetEnvironmentVariable(varName);

            if (string.IsNullOrEmpty(strVal))
            {
                value = string.Empty;
                return false;
            }

            value = strVal;
            _logger.Debug($"Loaded Variable from Environment : {varName}={value}");
            return true;
        }

        private void LoadEnv()
        {
            if (LoadInt("RCONPort", out int port))
                RCONPort = port;

            if (LoadInt("Delay", out int delay))
                Delay = delay;

            if (LoadInt("NumOfBackups", out int backups))
                NumOfBackups = backups;

            if (LoadInt("MapPort", out int mapPort))
                MapPort = mapPort;

            if (LoadInt("ServerPort", out int serverPort))
                ServerPort = serverPort;

            if (LoadInt("ControllerPort", out int controllerPort))
                ControllerPort = controllerPort;

            if (LoadStr("RCONPassword", out string rconPass))
                RCONPassword = rconPass;

            if (LoadStr("RCONHost", out string rconHost))
                RCONHost = rconHost;

            if (LoadStr("ServerName", out string serverName))
                ServerName = serverName;

            if (LoadStr("MapHealthUrl", out string mapHealth))
                MapHealthUrl = mapHealth;

            if (LoadStr("MapBrowserUrl", out string mapBrowser))
                MapBrowserUrl = mapBrowser;

            if (LoadStr("ServerContainerName", out string serverContainerName))
                ServerContainerName = serverContainerName;
        }

        private void LoadEnvFromFile()
        {
            string envPath = ".env";

            if (!Path.Exists(envPath))
                return;

            foreach (string line in File.ReadLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split('=');

                if (parts.Length != 2)
                    continue;

                var key = parts[0];
                var value = parts[1];

                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}