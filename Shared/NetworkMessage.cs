using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared {
    public enum MessageType {
        RoomList,
        CreateRoom,
        RoomCreated,
        RoomInfo,
        JoinRoom,
        RoomJoined,
        PlayerJoined,
        LeaveRoom,
        StartGame,
        GameStarted,
        InitMap,
        NewHost,
        PlayerLeft,
        RefreshRooms,
        InitPlayer,
        MovePlayer,
        RemovePlayer,
        PlaceBomb,
        ExplodeBomb,
        RespawnPlayer,
        Error,
    }

    public class NetworkMessage {
        public MessageType Type { get; private set; }
        public Dictionary<string, string> Data { get; private set; }

        public NetworkMessage(MessageType type, Dictionary<string, string> data) {
            Type = type;
            Data = data;
        }

        public string ToJson() {
            var options = new JsonSerializerOptions {
                Converters = { new JsonStringEnumConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(this, options);
        }

        public static NetworkMessage FromJson(string json) {
            var options = new JsonSerializerOptions {
                Converters = { new JsonStringEnumConverter() },
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<NetworkMessage>(json, options)!;
        }
    }
}
