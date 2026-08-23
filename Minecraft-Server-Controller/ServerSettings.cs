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

        public ServerSettings()
        {
            RCONHost = "server";
            RCONPort = 25575;
            RCONPassword = string.Empty;
            ServerName = string.Empty;
            Delay = 3000;
            NumOfBackups = 3;
            MapPort = 8123;
            ServerPort = 25565;
            ControllerPort = 8080;

            LoadEnv();
        }

        private void LoadEnv()
        {
            string? rconPort = Environment.GetEnvironmentVariable("RCONPort");

            if (!string.IsNullOrEmpty(rconPort))
            {
                if (int.TryParse(rconPort, out int port))
                    RCONPort = port;
            }

            string? delay = Environment.GetEnvironmentVariable("Delay");

            if (!string.IsNullOrEmpty(delay))
            {
                if (int.TryParse(delay, out int delayInt))
                    Delay = delayInt;
            }

            string? numOfBackups = Environment.GetEnvironmentVariable("NumOfBackups");

            if (!string.IsNullOrEmpty(numOfBackups))
            {
                if (int.TryParse(numOfBackups, out int backups))
                   NumOfBackups = backups;
            }

            string? mapPort = Environment.GetEnvironmentVariable("MapPort");

            if (!string.IsNullOrEmpty(mapPort))
            {
                if (int.TryParse(mapPort, out int mapPortInt))
                    MapPort = mapPortInt;
            }

            string? serverPort = Environment.GetEnvironmentVariable("ServerPort");

            if (!string.IsNullOrEmpty(serverPort))
            {
                if (int.TryParse(serverPort, out int serverPortInt))
                    ServerPort = serverPortInt;
            }

            string? controllerPort = Environment.GetEnvironmentVariable("ControllerPort");

            if (!string.IsNullOrEmpty(controllerPort))
            {
                if (int.TryParse(controllerPort, out int controllerPortInt))
                    ControllerPort = controllerPortInt;
            }

            string? rconPassword = Environment.GetEnvironmentVariable("RCONPassword");

            if (!string.IsNullOrEmpty(rconPassword))
                RCONPassword = rconPassword;

            string? rconHost = Environment.GetEnvironmentVariable("RCONHost");

            if (!string.IsNullOrEmpty(rconHost))
                RCONHost = rconHost;

            string? name = Environment.GetEnvironmentVariable("ServerName");

            if (!string.IsNullOrEmpty(name))
                ServerName = name;
        }
    }
}
