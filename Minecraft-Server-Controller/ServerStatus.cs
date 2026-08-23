
namespace Minecraft_Server_Controller
{
    public class ServerStatus
    {
        public bool Online;
        public string? Version;
        public int OnlinePlayers;
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
