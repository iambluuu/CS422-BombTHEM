using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared {
    public enum MessageDirection {
        Client,
        Server
    }

    public enum ClientMessageType {
        Ping,
        GetClientId,
        CreateRoom,
        GetRoomInfo,
        GetRoomList,
        RefreshRooms,
        AddBot,
        JoinRoom,
        LeaveRoom,
        StartGame,
        GetGameInfo,
        MovePlayer,
        PlaceBomb,
    }

    public enum ServerMessageType {
        Pong,
        ClientId,
        RoomCreated,
        RoomList,
        RoomInfo,
        RoomJoined,
        PlayerJoined,
        GameStarted,
        NewHost,
        PlayerLeft,
        GameInfo,
        PlayerMoved,
        BombPlaced,
        BombExploded,
        PlayerDied,
        Error
    }

    public class MessageType {
        public MessageDirection Direction { get; set; }
        public string Name { get; set; }

        public MessageType(MessageDirection direction, string name) {
            Direction = direction;
            Name = name;
        }

        public static MessageType From(ClientMessageType type) =>
            new(MessageDirection.Client, type.ToString());

        public static MessageType From(ServerMessageType type) =>
            new(MessageDirection.Server, type.ToString());

        public override string ToString() => $"{Direction}.{Name}";
    }

    public class NetworkMessage {
        public MessageType Type { get; private set; }
        public Dictionary<string, string> Data { get; private set; }

        [JsonConstructor]
        private NetworkMessage(MessageType type, Dictionary<string, string> data) {
            Type = type;
            Data = data;
        }

        public static NetworkMessage From(ClientMessageType type, Dictionary<string, string>? data = null) =>
            new(MessageType.From(type), data ?? []);

        public static NetworkMessage From(ServerMessageType type, Dictionary<string, string>? data = null) =>
            new(MessageType.From(type), data ?? []);

        public string ToJson() {
            var options = new JsonSerializerOptions {
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(this, options);
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
