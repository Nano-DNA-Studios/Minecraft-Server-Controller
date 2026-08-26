namespace Minecraft_Server_Controller
{
    public class ServerSettings
    {
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

            string? rconPassword = Environment.GetEnvironmentVariable("RCONPassword");

            if (!string.IsNullOrEmpty(rconPassword))
                RCONPassword = rconPassword;

            string? rconHost = Environment.GetEnvironmentVariable("RCONHost");

            if (!string.IsNullOrEmpty(rconHost))
                RCONHost = rconHost;

            string? name = Environment.GetEnvironmentVariable("ServerName");

            if (!string.IsNullOrEmpty(name))
                ServerName = name;

            string? mapHealth = Environment.GetEnvironmentVariable("MapHealthUrl");

            if (!string.IsNullOrEmpty(mapHealth))
                MapHealthUrl = mapHealth;

            string? mapBrowser = Environment.GetEnvironmentVariable("MapBrowserUrl");

            if (!string.IsNullOrEmpty(mapBrowser))
                MapBrowserUrl = mapBrowser;

            string? containerName = Environment.GetEnvironmentVariable("ServerContainerName");

            if (!string.IsNullOrEmpty(containerName))
                ServerContainerName = containerName;
        }
    }
}
