
namespace Minecraft_Server_Controller
{
    public class Player
    {
        public string Name { get; set; }

        public string ID { get; set; }

        public Player(string name, string id)
        {
            Name = name;
            ID = id;
        }
    }

    public class ServerStatus
    {
        public bool Online;
        public string? Version;
        public int OnlinePlayers;
        public Player[]? Players;
        public int MaxPlayers;
        public string? Motd;
        public TimeSpan Latency;
        public string? Error = null;

        public event Action? OnUpdated;

        public void Update()
        {
            OnUpdated?.Invoke();
        }
    }
}
