using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.PacketWriter;

namespace Shared {
    public enum MessageDirection : byte {
        Client,
        Server
    }

    public enum ClientMessageType : byte {
        Ping,
        GetClientId,
        SetUsername,
        GetUsername,
        CreateRoom,
        GetRoomInfo,
        GetRoomList,
        AddBot,
        KickPlayer,
        JoinRoom,
        LeaveRoom,
        StartGame,
        LeaveGame,
        GetGameInfo,
        MovePlayer,
        PlaceBomb,
        GetGameResults,
        UsePowerUp,
    }

    public enum ServerMessageType : byte {
        Pong,
        Connected,
        NotConnected,
        ClientId,
        UsernameSet,
        RoomCreated,
        RoomList,
        RoomInfo,
        RoomJoined,
        PlayerJoined,
        PlayerKicked,
        GameStarted,
        GameStopped,
        GameLeft,
        NewHost,
        PlayerLeft,
        GameInfo,
        PlayerMoved,
        BombPlaced,
        BombExploded,
        PlayerDied,
        GameResults,
        Error,
        ItemExpired,
        ItemSpawned,
        ItemPickedUp,
        PowerUpUsed,
        PowerUpExpired,
    }

    public class MessageType {
        public MessageDirection Direction { get; set; }
        public byte Name { get; set; }

        public MessageType(MessageDirection direction, byte name) {
            Direction = direction;
            Name = name;
        }

        public static MessageType From(ClientMessageType type) =>
            new(MessageDirection.Client, (byte)type);

        public static MessageType From(ServerMessageType type) =>
            new(MessageDirection.Server, (byte)type);

        public override string ToString() => $"{Direction}.{Name}";
    }

    public class NetworkMessage {
        public MessageType Type { get; private set; }
        public Dictionary<byte, object> Data { get; private set; }

        [JsonConstructor]
        private NetworkMessage(MessageType type, Dictionary<byte, object> data) {
            Type = type;
            Data = data;
        }

        public static NetworkMessage From(ClientMessageType type, Dictionary<byte, object>? data = null) =>
            new(MessageType.From(type), data ?? []);

        public static NetworkMessage From(ServerMessageType type, Dictionary<byte, object>? data = null) =>
            new(MessageType.From(type), data ?? []);

        public string ToJson() {
            var options = new JsonSerializerOptions {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(this, options);
        }

        public byte[] ToBytes() {
            // [direction][name][data]| 
            MemoryStream ms = new();
            BinaryWriter writer = new(ms);
            writer.Write((byte)Type.Direction);
            writer.Write(Type.Name);

            if (Data != null && Data.Count > 0) {
                switch (Type.Direction) {
                    case MessageDirection.Client:
                        ClientPacket.WriteContent(writer, (ClientMessageType)Type.Name, Data);
                        break;
                    case MessageDirection.Server:
                        ServerPacket.WriteContent(writer, (ServerMessageType)Type.Name, Data);
                        break;
                }
            }
            writer.Write('|'); // End of message marker
            // Print the bytes for debugging
            // Console.WriteLine($"NetworkMessage.ToBytes: {BitConverter.ToString(ms.ToArray())}");
            return ms.ToArray();
        }

        public static NetworkMessage FromBytes(byte[] bytes) {
            using MemoryStream ms = new(bytes);
            using BinaryReader reader = new(ms);

            // Console.WriteLine($"NetworkMessage.FromBytes: {BitConverter.ToString(bytes)}");

            var direction = (MessageDirection)reader.ReadByte();
            var name = reader.ReadByte();

            var data = direction switch {
                MessageDirection.Client => ClientPacket.ReadContent(reader),
                MessageDirection.Server => ServerPacket.ReadContent(reader),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };

            var newMessage = new NetworkMessage(new MessageType(direction, name), data);
            // Print the message for debugging
            // Console.WriteLine($"NetworkMessage.FromBytes: {newMessage.ToJson()}");
            return newMessage;
        }

        public static NetworkMessage FromJson(string json) {
            var options = new JsonSerializerOptions {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<NetworkMessage>(json, options)!;
        }
    }

}
