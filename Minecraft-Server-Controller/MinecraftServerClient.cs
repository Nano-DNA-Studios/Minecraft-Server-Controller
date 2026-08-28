using NLog;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Minecraft_Server_Controller
{
    public class MinecraftServerClient
    {
        public CancellationTokenSource CancellationTokenSource { get; set; }

        private ServerSettings _ServerSettings { get; set; }

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public MinecraftServerClient(ServerSettings settings)
        {
            _ServerSettings = settings;
            CancellationTokenSource = new CancellationTokenSource();
        }

        public async Task PingServer(ServerStatus status)
        {
            _logger.Trace("Pinging Minecraft server at {Host}:{Port}.", _ServerSettings.RCONHost, _ServerSettings.ServerPort);

            CancellationToken token = CancellationTokenSource.Token;

            TcpClient client = new();

            await client.ConnectAsync(_ServerSettings.RCONHost, _ServerSettings.ServerPort);

            using NetworkStream stream = client.GetStream();
            
            await SendHandshakeAsync(stream, _ServerSettings.RCONHost, _ServerSettings.ServerPort, token);
            await SendStatusRequestAsync(stream, token);

            string json = await ReadStatusResponseAsync(stream, token);

            Stopwatch stopwatch = Stopwatch.StartNew();

            long payload = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            await SendPingAsync(stream, payload, token);
            await ReadPongAsync(stream, payload, token);

            stopwatch.Stop();

            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("version", out JsonElement versionElement))
            {
                if (versionElement.TryGetProperty("name", out JsonElement nameElement))
                    status.Version = nameElement.GetString();
            }

            if (root.TryGetProperty("players", out JsonElement playersElement))
            {
                if (playersElement.TryGetProperty("online", out JsonElement onlineElement))
                    status.OnlinePlayers = onlineElement.GetInt32();

                if (playersElement.TryGetProperty("max", out JsonElement maxElement))
                    status.MaxPlayers = maxElement.GetInt32();

                if (playersElement.TryGetProperty("sample", out JsonElement allPlayers))
                {
                    List<Player> players = new List<Player>();

                    foreach (JsonElement playerElement in allPlayers.EnumerateArray())
                    {
                        string? name = playerElement.GetProperty("name").GetString();
                        string? id = playerElement.GetProperty("id").GetString();

                        if (name == null || id == null)
                            continue;

                        players.Add(new Player(name, id));
                    }

                    if (players.Count > 0)
                        status.Players = players.ToArray();
                }
            }

            if (status.OnlinePlayers == 0)
                status.Players = new Player[0];

            if (root.TryGetProperty("description", out JsonElement descriptionElement))
                status.Motd = ReadDescription(descriptionElement);

            status.Online = true;
            status.Latency = stopwatch.Elapsed;

            _logger.Trace("Minecraft status received: OnlinePlayers={OnlinePlayers}, MaxPlayers={MaxPlayers}, Version={Version}, LatencyMs={LatencyMs}.",
                status.OnlinePlayers,
                status.MaxPlayers,
                status.Version,
                status.Latency.TotalMilliseconds
            );
        }

        private static async Task SendStatusRequestAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var packet = new MemoryStream();

            WriteVarInt(packet, 0x00); // Packet ID: Status Request

            await WritePacketAsync(stream, packet.ToArray(), cancellationToken);
        }

        private static async Task SendHandshakeAsync(Stream stream, string host, int port, CancellationToken cancellationToken)
        {
            using var packet = new MemoryStream();

            WriteVarInt(packet, 0x00); // Packet ID: Handshake
            WriteVarInt(packet, 0);    // Protocol version

            WriteString(packet, host);

            // Minecraft ports are written as unsigned 16-bit big-endian integers.
            packet.WriteByte((byte)(port >> 8));
            packet.WriteByte((byte)(port & 0xFF));

            WriteVarInt(packet, 1); // Next state: Status

            await WritePacketAsync(stream, packet.ToArray(), cancellationToken);
        }

        private static async Task<string> ReadStatusResponseAsync(Stream stream, CancellationToken cancellationToken)
        {
            int packetLength = await ReadVarIntAsync(stream, cancellationToken);
            byte[] packet = await ReadExactlyAsync(
                stream,
                packetLength,
                cancellationToken);

            using var packetStream = new MemoryStream(packet);

            int packetId = ReadVarInt(packetStream);

            if (packetId != 0x00)
                throw new InvalidDataException($"Expected status response packet 0x00, received 0x{packetId:X2}.");

            int jsonLength = ReadVarInt(packetStream);

            byte[] jsonBytes = new byte[jsonLength];

            int bytesRead = packetStream.Read(jsonBytes, 0, jsonBytes.Length);

            if (bytesRead != jsonLength)
                throw new EndOfStreamException("Incomplete Minecraft status response.");

            return Encoding.UTF8.GetString(jsonBytes);
        }

        private static async Task WritePacketAsync(Stream stream, byte[] packetData, CancellationToken cancellationToken)
        {
            using var framedPacket = new MemoryStream();

            WriteVarInt(framedPacket, packetData.Length);
            framedPacket.Write(packetData);

            await stream.WriteAsync(framedPacket.ToArray(), cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        private static void WriteString(Stream stream, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);

            WriteVarInt(stream, bytes.Length);
            stream.Write(bytes);
        }

        private static void WriteVarInt(Stream stream, int value)
        {
            uint unsignedValue = unchecked((uint)value);

            do
            {
                byte currentByte = (byte)(unsignedValue & 0x7F);
                unsignedValue >>= 7;

                if (unsignedValue != 0)
                    currentByte |= 0x80;

                stream.WriteByte(currentByte);
            }
            while (unsignedValue != 0);
        }

        private static int ReadVarInt(Stream stream)
        {
            int value = 0;
            int position = 0;

            while (true)
            {
                int rawByte = stream.ReadByte();

                if (rawByte == -1)
                    throw new EndOfStreamException();

                byte currentByte = (byte)rawByte;

                value |= (currentByte & 0x7F) << position;

                if ((currentByte & 0x80) == 0)
                    return value;

                position += 7;

                if (position >= 32)
                    throw new InvalidDataException("VarInt is too large.");
            }
        }

        private static async Task<int> ReadVarIntAsync(Stream stream, CancellationToken cancellationToken)
        {
            int value = 0;
            int position = 0;
            byte[] buffer = new byte[1];

            while (true)
            {
                int count = await stream.ReadAsync(buffer, cancellationToken);

                if (count == 0)
                    throw new EndOfStreamException();

                byte currentByte = buffer[0];

                value |= (currentByte & 0x7F) << position;

                if ((currentByte & 0x80) == 0)
                    return value;

                position += 7;

                if (position >= 32)
                    throw new InvalidDataException("VarInt is too large.");
            }
        }

        private static async Task<byte[]> ReadExactlyAsync(Stream stream, int length, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[length];
            int totalRead = 0;

            while (totalRead < length)
            {
                int count = await stream.ReadAsync(buffer.AsMemory(totalRead, length - totalRead), cancellationToken);

                if (count == 0)
                    throw new EndOfStreamException();

                totalRead += count;
            }

            return buffer;
        }

        private static void WriteInt64BigEndian(Stream stream, long value)
        {
            ulong unsignedValue = unchecked((ulong)value);

            for (int shift = 56; shift >= 0; shift -= 8)
                stream.WriteByte((byte)(unsignedValue >> shift));
        }

        private static long ReadInt64BigEndian(Stream stream)
        {
            ulong value = 0;

            for (int i = 0; i < 8; i++)
            {
                int currentByte = stream.ReadByte();

                if (currentByte == -1)
                    throw new EndOfStreamException();

                value = (value << 8) | (byte)currentByte;
            }

            return unchecked((long)value);
        }

        private static string? ReadDescription(JsonElement description)
        {
            if (description.ValueKind == JsonValueKind.String)
                return description.GetString();

            if (description.ValueKind == JsonValueKind.Object &&
                description.TryGetProperty("text", out JsonElement textElement))
                return textElement.GetString();

            return description.ToString();
        }

        private static async Task SendPingAsync(Stream stream, long payload, CancellationToken cancellationToken)
        {
            using var packet = new MemoryStream();

            WriteVarInt(packet, 0x01); // Packet ID: Ping
            WriteInt64BigEndian(packet, payload);

            await WritePacketAsync(stream, packet.ToArray(), cancellationToken);
        }

        private static async Task ReadPongAsync(Stream stream, long expectedPayload, CancellationToken cancellationToken)
        {
            int packetLength = await ReadVarIntAsync(stream, cancellationToken);
            byte[] packet = await ReadExactlyAsync(stream, packetLength, cancellationToken);

            using var packetStream = new MemoryStream(packet);

            int packetId = ReadVarInt(packetStream);

            if (packetId != 0x01)
                throw new InvalidDataException($"Expected pong packet 0x01, received 0x{packetId:X2}.");

            long payload = ReadInt64BigEndian(packetStream);

            if (payload != expectedPayload)
                throw new InvalidDataException("Pong payload did not match ping payload.");
        }
    }
}
