using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

using Shared;

namespace Server {
    public class Server {
        private TcpListener? _server;
        private readonly List<ClientHandler> _clients = [];
        private readonly Dictionary<string, GameRoom> _rooms = [];
        private int _nextPlayerId = 0;
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly Dictionary<string, ReaderWriterLockSlim> _roomLocks = [];
        private const int MAX_PLAYERS_PER_ROOM = 4;

        public class ClientHandler {
            public int PlayerId { get; set; }
            public required TcpClient Client { get; set; }
            public required NetworkStream Stream { get; set; }
            public string? RoomId { get; set; }
        }

        public class GameRoom {
            public string RoomId { get; set; }
            public int HostPlayerId { get; set; }
            public List<int> PlayerIds { get; set; } = new List<int>();
            public Map Map { get; set; }
            public bool GameStarted { get; set; } = false;
            public Thread? BombThread { get; set; }

            public GameRoom(string roomId, int hostPlayerId, Map map) {
                RoomId = roomId;
                HostPlayerId = hostPlayerId;
                Map = map;
                PlayerIds.Add(hostPlayerId);
            }
        }

        public void Start() {
            _server = new TcpListener(IPAddress.Any, 5000);
            _server.Start();
            Console.WriteLine("Server is running on port 5000...");

            while (true) {
                try {
                    TcpClient client = _server.AcceptTcpClient();
                    int playerId = _nextPlayerId++;

                    ClientHandler clientHandler = new ClientHandler {
                        PlayerId = playerId,
                        Client = client,
                        Stream = client.GetStream()
                    };

                    lock (_lock) {
                        _clients.Add(clientHandler);
                    }

                    Console.WriteLine($"Client connected: Player {playerId}");

                    // Send available rooms to the client
                    // SendRoomListToClient(playerId);

                    Thread clientThread = new Thread(HandleClient);
                    clientThread.IsBackground = true;
                    clientThread.Start(clientHandler);
                } catch (Exception ex) {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private void SendRoomListToClient(int playerId) {
            lock (_lock) {
                var roomList = _rooms.Values
                    .Where(r => !r.GameStarted)
                    .Select(r => $"{r.RoomId}:{r.PlayerIds.Count}/{MAX_PLAYERS_PER_ROOM}")
                    .ToList();

                SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomList, new() {
                    { "rooms", string.Join(";", roomList) }
                }));
            }
        }

        private Map GenerateRandomMap() {
            int height = 15;
            int width = 15;
            Map map = new Map(height, width);

            if (Utils.RandomInt(0, 2) < 0) {
                for (int i = 0; i < height; i++) {
                    map.SetTile(i, 0, TileType.Wall);
                    map.SetTile(i, width - 1, TileType.Wall);
                }

                for (int j = 1; j < width - 1; j++) {
                    map.SetTile(0, j, TileType.Wall);
                    map.SetTile(height - 1, j, TileType.Wall);
                }

                for (int i = 0; i < 100; i++) {
                    int x = Utils.RandomInt(1, height - 1);
                    int y = Utils.RandomInt(1, width - 1);
                    map.SetTile(x, y, TileType.Wall);
                }
            } else {
                for (int i = 0; i < height; i++) {
                    for (int j = 0; j < width; j++) {
                        map.SetTile(i, j, TileType.Wall);
                    }
                }

                bool[,] visited = new bool[height, width];
                List<(int x, int y, int fromX, int fromY)> walls = [];

                int startX = Utils.RandomInt(height / 2) * 2 + 1;
                int startY = Utils.RandomInt(width / 2) * 2 + 1;

                visited[startX, startY] = true;
                map.SetTile(startX, startY, TileType.Empty);

                foreach (var (dx, dy) in new[] { (2, 0), (-2, 0), (0, 2), (0, -2) }) {
                    int nx = startX + dx;
                    int ny = startY + dy;
                    if (map.IsInBounds(nx, ny)) {
                        walls.Add((nx, ny, startX, startY));
                    }
                }

                while (walls.Count > 0) {
                    int index = Utils.RandomInt(walls.Count);
                    var (x, y, fromX, fromY) = walls[index];
                    walls.RemoveAt(index);

                    if (!map.IsInBounds(x, y) || visited[x, y]) continue;

                    visited[x, y] = true;
                    map.SetTile(x, y, TileType.Empty);

                    int wallX = (x + fromX) / 2;
                    int wallY = (y + fromY) / 2;
                    map.SetTile(wallX, wallY, TileType.Empty);

                    foreach (var (dx, dy) in new[] { (2, 0), (-2, 0), (0, 2), (0, -2) }) {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (map.IsInBounds(nx, ny) && !visited[nx, ny]) {
                            walls.Add((nx, ny, x, y));
                        }
                    }
                }
            }

            return map;
        }

        private void ProcessBombs(string roomId) {
            while (true) {
                GameRoom? room;
                lock (_roomLocks[roomId]) {
                    if (!_rooms.TryGetValue(roomId, out room) || !room.GameStarted) {
                        return;
                    }

                    try {
                        List<Bomb> explodedBombs = [];
                        foreach (var bomb in room.Map.Bombs) {
                            if ((DateTime.Now - bomb.PlaceTime).TotalSeconds >= 2.0) {
                                explodedBombs.Add(bomb);
                            }
                        }

                        foreach (var bomb in explodedBombs) {
                            room.Map.ExplodeBomb(bomb.Position.X, bomb.Position.Y);

                            BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.BombExploded, new() {
                                { "x", bomb.Position.X.ToString() },
                                { "y", bomb.Position.Y.ToString() },
                                { "positions", string.Join(";", bomb.ExplosionPositions) }
                            }));

                            foreach (var pos in bomb.ExplosionPositions) {
                                CheckForPlayersInExplosion(room, pos.X, pos.Y);
                            }

                            room.Map.Bombs.Remove(bomb);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Error processing bombs for room {roomId}: {ex.Message}");
                    }
                }

                Thread.Sleep(100);
            }
        }

        private void CheckForPlayersInExplosion(GameRoom room, int x, int y) {
            foreach (var player in room.Map.PlayerPositions) {
                Position playerPos = player.Value;
                if (playerPos.X == x && playerPos.Y == y) {
                    Position respawnPosition = GenerateInitialPosition(room.Map);
                    room.Map.SetPlayerPosition(player.Key, respawnPosition.X, respawnPosition.Y);

                    BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.PlayerDied, new() {
                        { "playerId", player.Key.ToString() },
                        { "x", respawnPosition.X.ToString() },
                        { "y", respawnPosition.Y.ToString() },
                        { "d", Direction.Down.ToString() }
                    }));
                }
            }
        }

        private Position GenerateInitialPosition(Map map) {
            int x, y;
            do {
                x = Utils.RandomInt(0, map.Height);
                y = Utils.RandomInt(0, map.Width);
            } while (map.GetTile(x, y) != TileType.Empty);

            return new Position(x, y);
        }

        private void HandleClient(object? obj) {
            if (obj is not ClientHandler handler)
                throw new ArgumentNullException(nameof(obj), "Expected a non-null ClientHandler object");

            NetworkStream stream = handler.Stream;
            byte[] buffer = new byte[1024];

            try {
                while (true) {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] messages = message.Split(['\n', '\r', '|'], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var msg in messages) {
                        if (!string.IsNullOrEmpty(msg)) {
                            // Console.WriteLine($"Received message from player {handler.PlayerId}: {msg}");
                            ProcessClientMessage(handler.PlayerId, NetworkMessage.FromJson(msg));
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error handling client {handler.PlayerId}: {ex.Message}");
            } finally {
                DisconnectClient(handler);
            }
        }

        private void DisconnectClient(ClientHandler handler) {
            lock (_lock) {
                _clients.Remove(handler);

                if (handler.RoomId != null && _rooms.TryGetValue(handler.RoomId, out GameRoom? room)) {
                    room.PlayerIds.Remove(handler.PlayerId);

                    // If the host disconnected, assign a new host or close the room
                    if (room.HostPlayerId == handler.PlayerId) {
                        if (room.PlayerIds.Count > 0) {
                            room.HostPlayerId = room.PlayerIds[0];
                            BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.NewHost, new() {
                                { "hostId", room.HostPlayerId.ToString() }
                            }));
                        } else {
                            _rooms.Remove(room.RoomId);
                            if (room.BombThread != null) {
                                // Thread will exit on its own as the room is removed
                            }

                            // Broadcast room list update to all clients in lobby
                            BroadcastRoomListToLobby();
                        }
                    }

                    // Remove player from the game
                    if (room.GameStarted) {
                        room.Map.PlayerPositions.Remove(handler.PlayerId);
                        BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.PlayerLeft, new() {
                            { "playerId", handler.PlayerId.ToString() }
                        }));
                    } else {
                        // If game hasn't started, notify other players in room
                        BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.PlayerLeft, new() {
                            { "playerId", handler.PlayerId.ToString() }
                        }));

                        // Update room list for players in lobby
                        BroadcastRoomListToLobby();
                    }
                }
            }

            Console.WriteLine($"Player {handler.PlayerId} disconnected");

            try {
                handler.Stream.Close();
                handler.Client.Close();
            } catch { }
        }

        private void BroadcastRoomListToLobby() {
            // Send updated room list to all clients not in a room
            var lobbyClients = _clients.Where(c => c.RoomId == null).ToList();
            foreach (var client in lobbyClients) {
                SendRoomListToClient(client.PlayerId);
            }
        }

        private void ProcessClientMessage(int playerId, NetworkMessage message) {
            ClientHandler? client = null;

            lock (_lock) {
                client = _clients.Find(c => c.PlayerId == playerId);
                if (client == null) return;
            }

            switch (Enum.Parse<ClientMessageType>(message.Type.Name)) {
                case ClientMessageType.GetClientId: {
                        SendToClient(playerId, NetworkMessage.From(ServerMessageType.ClientId, new() {
                            { "clientId", playerId.ToString() }
                        }));
                        break;
                    }
                case ClientMessageType.CreateRoom: {
                        if (client.RoomId != null) return;

                        string roomId = message.Data.ContainsKey("roomId") ? message.Data["roomId"] : Guid.NewGuid().ToString().Substring(0, 6);

                        lock (_lock) {
                            if (_rooms.ContainsKey(roomId)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Room with this ID already exists" }
                                }));
                                return;
                            }

                            GameRoom newRoom = new GameRoom(roomId, playerId, GenerateRandomMap());
                            _rooms.Add(roomId, newRoom);
                            _roomLocks.Add(roomId, new ReaderWriterLockSlim());

                            client.RoomId = roomId;

                            SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomCreated, new() {
                                { "roomId", roomId },
                            }));

                            // Update room list for all clients in lobby
                            // BroadcastRoomListToLobby();
                        }
                        break;
                    }
                case ClientMessageType.GetRoomInfo: {
                        if (client.RoomId == null) return; // Not in a room

                        string roomId = client.RoomId;

                        lock (_roomLocks[roomId]) {
                            if (_rooms.TryGetValue(roomId, out GameRoom? room)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomInfo, new() {
                                    { "roomId", roomId },
                                    { "hostId", room.HostPlayerId.ToString() },
                                    { "isHost", (playerId == room.HostPlayerId).ToString().ToLower() },
                                    { "playerIds", string.Join(";", room.PlayerIds) },
                                }));
                            }
                        }
                        break;
                    }
                case ClientMessageType.JoinRoom: {
                        if (client.RoomId != null) return; // Already in a room

                        string roomId = message.Data["roomId"];

                        lock (_lock) {
                            if (!_rooms.ContainsKey(roomId)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Room not found" }
                                }));
                                return;
                            }
                        }

                        lock (_roomLocks[roomId]) {
                            _rooms.TryGetValue(roomId, out GameRoom? room);
                            if (room == null) {
                                return;
                            }

                            if (room.GameStarted) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Game already in progress" }
                                }));
                                return;
                            }

                            if (room.PlayerIds.Count >= MAX_PLAYERS_PER_ROOM) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Room is full" }
                                }));
                                return;
                            }

                            room.PlayerIds.Add(playerId);
                            client.RoomId = roomId;

                            // Notify the player that they joined the room
                            SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomJoined, new() {
                                { "roomId", roomId}
                            }));

                            // Notify other players in the room that a new player joined
                            foreach (var existingPlayerId in room.PlayerIds) {
                                if (existingPlayerId != playerId) {
                                    SendToClient(existingPlayerId, NetworkMessage.From(ServerMessageType.PlayerJoined, new() {
                                        { "playerId", playerId.ToString() },
                                    }));
                                }
                            }

                            // Update room list for all clients in lobby
                            // BroadcastRoomListToLobby();
                        }
                        break;
                    }
                case ClientMessageType.LeaveRoom: {
                        if (client.RoomId == null) return; // Not in a room

                        string roomId = client.RoomId;

                        lock (_roomLocks[roomId]) {
                            if (_rooms.TryGetValue(roomId, out GameRoom? room)) {
                                room.PlayerIds.Remove(playerId);

                                // If the host left, assign a new host or close the room
                                if (room.HostPlayerId == playerId) {
                                    if (room.PlayerIds.Count > 0) {
                                        room.HostPlayerId = room.PlayerIds[0];
                                        BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.NewHost, new() {
                                            { "hostId", room.HostPlayerId.ToString() }
                                        }));
                                    } else {
                                        _rooms.Remove(roomId);
                                        _roomLocks.Remove(roomId);
                                        if (room.BombThread != null) {
                                            // Thread will exit on its own as the room is removed
                                        }
                                    }
                                }

                                // Notify other players that this player left
                                BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.PlayerLeft, new() {
                                { "playerId", playerId.ToString() }
                            }));
                            }

                            client.RoomId = null;

                            // Send room list to the player who left
                            SendRoomListToClient(playerId);

                            // Update room list for all clients in lobby
                            // BroadcastRoomListToLobby();
                        }
                        break;
                    }
                case ClientMessageType.StartGame: {
                        if (client.RoomId == null) return; // Not in a room

                        string roomId = client.RoomId;

                        lock (_roomLocks[roomId]) {
                            if (!_rooms.TryGetValue(roomId, out GameRoom? room)) return;

                            // Only the host can start the game
                            if (room.HostPlayerId != playerId) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                { "message", "Only the host can start the game" }
                            }));
                                return;
                            }

                            // Start the game
                            room.BombThread = new Thread(() => ProcessBombs(roomId)) {
                                IsBackground = true
                            };
                            room.BombThread.Start();
                            for (int i = 0; i < room.PlayerIds.Count; i++) {
                                Position initialPosition = GenerateInitialPosition(room.Map);
                                room.Map.SetPlayerPosition(room.PlayerIds[i], initialPosition.X, initialPosition.Y);
                            }

                            room.GameStarted = true;

                            // Initialize player positions
                            foreach (var pid in room.PlayerIds) {
                                Position initialPosition = GenerateInitialPosition(room.Map);
                                room.Map.SetPlayerPosition(pid, initialPosition.X, initialPosition.Y);
                            }

                            // Send map to all players
                            BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.GameStarted));

                            // Update room list for all clients in lobby since this room is no longer joinable
                            // BroadcastRoomListToLobby();
                        }
                        break;
                    }
                case ClientMessageType.GetGameInfo: {
                        if (client.RoomId == null) return; // Not in a room

                        string roomId = client.RoomId;

                        lock (_roomLocks[roomId]) {
                            if (_rooms.TryGetValue(roomId, out GameRoom? room)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.GameInfo, new() {
                                    { "map", room.Map.ToString() },
                                    { "playerCount", room.PlayerIds.Count.ToString() },
                                    { "playerIds", string.Join(";", room.PlayerIds) },
                                    { "playerPositions", string.Join(";", room.Map.PlayerPositions.Select(p => new Position(p.Value.X, p.Value.Y).ToString())) },
                                }));
                            }
                        }
                        break;
                    }
                case ClientMessageType.MovePlayer: {
                        if (client.RoomId == null) return; // Not in a room

                        string roomId = client.RoomId;

                        lock (_roomLocks[roomId]) {
                            if (!_rooms.TryGetValue(roomId, out GameRoom? room) || !room.GameStarted) return;

                            Direction direction = Enum.Parse<Direction>(message.Data["direction"]);
                            room.Map.MovePlayer(playerId, direction);

                            BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.PlayerMoved, new() {
                                { "playerId", playerId.ToString() },
                                { "x", room.Map.GetPlayerPosition(playerId).X.ToString() },
                                { "y", room.Map.GetPlayerPosition(playerId).Y.ToString() },
                                { "d", direction.ToString() }
                            }));
                        }
                        break;
                    }
                case ClientMessageType.PlaceBomb: {
                        if (client.RoomId == null) return; // Not in a room

                        string roomId = client.RoomId;

                        lock (_roomLocks[roomId]) {
                            if (!_rooms.TryGetValue(roomId, out GameRoom? room) || !room.GameStarted) return;

                            BombType type = Enum.Parse<BombType>(message.Data["type"]);

                            int x = room.Map.GetPlayerPosition(playerId).X;
                            int y = room.Map.GetPlayerPosition(playerId).Y;
                            if (!room.Map.HasBomb(x, y)) {
                                room.Map.AddBomb(x, y, type);

                                BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.BombPlaced, new() {
                                { "x", x.ToString() },
                                { "y", y.ToString() },
                                { "type", type.ToString() }
                            }));
                            }
                        }
                        break;
                    }
                case ClientMessageType.RefreshRooms: {
                        SendRoomListToClient(playerId);
                        break;
                    }
            }
        }

        private void BroadcastToRoom(string roomId, NetworkMessage message) {
            byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");

            lock (_lock) {
                if (!_rooms.TryGetValue(roomId, out GameRoom? room)) return;

                foreach (var playerId in room.PlayerIds) {
                    var client = _clients.Find(c => c.PlayerId == playerId);
                    if (client != null) {
                        try {
                            client.Stream.Write(data, 0, data.Length);
                        } catch {
                            // Ignore errors
                        }
                    }
                }
            }
        }

        private void BroadcastToAll(NetworkMessage message) {
            byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");

            lock (_lock) {
                foreach (var client in _clients) {
                    try {
                        client.Stream.Write(data, 0, data.Length);
                    } catch {
                        // Ignore errors
                    }
                }
            }
        }

        private void SendToClient(int playerId, NetworkMessage message) {
            if (message.Type.Direction != MessageDirection.Server) {
                Console.WriteLine("The sent message must be from the server side");
                return;
            }

            lock (_lock) {
                var client = _clients.Find(c => c.PlayerId == playerId);
                if (client != null) {
                    try {
                        byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");
                        client.Stream.Write(data, 0, data.Length);
                    } catch (Exception ex) {
                        Console.WriteLine($"Error sending to client {client.PlayerId}: {ex.Message}");
                    }
                } else {
                    Console.WriteLine($"Client {playerId} not found for sending message.");
                }
            }
        }
    }
}
