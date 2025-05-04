using System.Net;
using System.Net.Sockets;
using System.Text;

using Shared;

namespace Server {
    public class Server {
        private TcpListener? _server;
        private readonly List<ClientHandler> _clients = [];
        private readonly List<GameBot> _bots = [];
        private readonly Dictionary<string, GameRoom> _rooms = [];
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly Dictionary<string, ReaderWriterLockSlim> _roomLocks = [];
        private const int MAX_PLAYERS_PER_ROOM = 4;

        public class ClientHandler {
            public int PlayerId { get; set; }
            public required TcpClient Client { get; set; }
            public required NetworkStream Stream { get; set; }
            public Thread Thread { get; set; } = null!;
            public CancellationTokenSource Cts { get; set; } = null!;
            public bool connected { get; set; } = true;
            public string? RoomId { get; set; }
        }

        public class GameRoom {
            public string RoomId { get; set; }
            public int HostPlayerId { get; set; }
            public List<int> PlayerIds { get; set; } = [];
            public Map Map { get; set; }
            public bool GameStarted { get; set; } = false;
            public Thread? BombThread { get; set; } = null;
            public CancellationTokenSource? BombCts { get; set; } = null;

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
                    int playerId = GeneratePlayerId();

                    ClientHandler clientHandler = new() {
                        PlayerId = playerId,
                        Client = client,
                        Stream = client.GetStream()
                    };

                    lock (_lock) {
                        _clients.Add(clientHandler);
                    }

                    Console.WriteLine($"Client connected: Player {playerId}");

                    clientHandler.Cts = new CancellationTokenSource();
                    clientHandler.Thread = new(() => HandleClient(clientHandler)) {
                        IsBackground = true
                    };
                    clientHandler.Thread.Start();
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

        private bool isBot(int playerId) {
            return playerId < 100000;
        }

        private int GeneratePlayerId() {
            lock (_lock) {
                while (true) {
                    int playerId = Utils.RandomInt(100000, 1000000);
                    if (!_clients.Any(c => c.PlayerId == playerId)) {
                        return playerId;
                    }
                }
            }
        }

        private int GenerateBotId() {
            lock (_lock) {
                while (true) {
                    int botId = Utils.RandomInt(100000);
                    if (!_bots.Any(b => b.BotId == botId)) {
                        return botId;
                    }
                }
            }
        }

        private string GenerateRoomId() {
            lock (_lock) {
                while (true) {
                    string roomId = string.Empty;
                    for (int i = 0; i < 6; i++) {
                        roomId += (char)(Utils.RandomInt(26) + 'A');
                    }

                    if (!_rooms.ContainsKey(roomId)) {
                        return roomId;
                    }
                }
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
            GameRoom? room;
            lock (_roomLocks[roomId]) {
                if (!_rooms.TryGetValue(roomId, out room) || !room.GameStarted) {
                    return;
                }
            }

            while (room.BombCts != null && !room.BombCts.Token.IsCancellationRequested) {
                try {
                    lock (_roomLocks[roomId]) {
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
                                CheckForPlayersInExplosion(room, pos.X, pos.Y, bomb.PlayerId);
                            }

                            room.Map.Bombs.Remove(bomb);
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing bombs for room {roomId}: {ex.Message}");
                }

                Thread.Sleep(100);
            }
        }

        private void CheckForPlayersInExplosion(GameRoom room, int x, int y, int bombPlayerId) {
            foreach (var player in room.Map.PlayerPositions) {
                Position playerPos = player.Value;
                if (playerPos.X == x && playerPos.Y == y) {
                    Position respawnPosition = GenerateInitialPosition(room.Map);
                    room.Map.SetPlayerPosition(player.Key, respawnPosition.X, respawnPosition.Y);

                    BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.PlayerDied, new() {
                        { "playerId", player.Key.ToString() },
                        { "byPlayerId", bombPlayerId.ToString() },
                        { "x", respawnPosition.X.ToString() },
                        { "y", respawnPosition.Y.ToString() },
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

        private async void HandleClient(ClientHandler handler) {
            NetworkStream stream = handler.Stream;
            byte[] buffer = new byte[4096];

            MemoryStream messageBuffer = new MemoryStream();
            try {
                while (!handler.Cts.Token.IsCancellationRequested) {
                    int bytesRead;
                    try {
                        var readTask = handler.Stream.ReadAsync(buffer, 0, buffer.Length, handler.Cts.Token);
                        bytesRead = await readTask;
                    } catch (Exception ex) {
                        Console.WriteLine($"Error reading from client {handler.PlayerId}: {ex.Message}");
                        break;
                    }

                    if (bytesRead <= 0) {
                        break;
                    }

                    messageBuffer.Write(buffer, 0, bytesRead);

                    ProcessMessageBuffer(handler.PlayerId, messageBuffer);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error handling client {handler.PlayerId}: {ex.Message}");
            } finally {
                messageBuffer.Dispose();
                DisconnectClient(handler);
            }
        }

        private void ProcessMessageBuffer(int playerId, MemoryStream messageBuffer) {
            messageBuffer.Position = 0;
            long processedPosition = 0;
            byte[] bufferData = messageBuffer.ToArray();
            while (true) {
                int delimiterIndex = -1;
                for (int i = (int)processedPosition; i < bufferData.Length; i++) {
                    if (bufferData[i] == '|') {
                        delimiterIndex = i;
                        break;
                    }
                }

                if (delimiterIndex == -1) break;

                int messageSize = delimiterIndex - (int)processedPosition;

                const int MaxMessageSize = 1024 * 1024;
                if (messageSize > MaxMessageSize) {
                    Console.WriteLine($"Message from client {playerId} exceeds maximum allowed size ({messageSize} > {MaxMessageSize})");
                    messageBuffer.SetLength(0);
                    return;
                }

                byte[] messageData = new byte[messageSize];
                Array.Copy(bufferData, processedPosition, messageData, 0, messageSize);
                string messageString = Encoding.UTF8.GetString(messageData);

                try {
                    if (!string.IsNullOrEmpty(messageString)) {
                        Console.WriteLine($"Received message from client {playerId}: {messageString}");
                        NetworkMessage messageObj = NetworkMessage.FromJson(messageString);
                        ProcessClientMessage(playerId, messageObj);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing message from client {playerId}: {ex.Message}");
                }

                processedPosition = delimiterIndex + 1;
            }

            if (processedPosition >= bufferData.Length) {
                messageBuffer.SetLength(0);
            } else if (processedPosition > 0) {
                byte[] remainingData = new byte[bufferData.Length - processedPosition];
                Array.Copy(bufferData, processedPosition, remainingData, 0, remainingData.Length);
                messageBuffer.SetLength(0);
                messageBuffer.Write(remainingData, 0, remainingData.Length);
            }
        }

        private void DisconnectClient(ClientHandler handler) {
            RemoveClientFromRoom(handler);

            try {
                handler.Cts.Cancel();
                handler.Thread.Join();
                handler.Stream.Close();
                handler.Client.Close();
                handler.connected = false;
            } catch (Exception ex) {
                Console.WriteLine($"Error disconnecting client {handler.PlayerId}: {ex.Message}");
            }

            lock (_lock) {
                _clients.RemoveAll(c => c.PlayerId == handler.PlayerId);
            }

            Console.WriteLine($"Player {handler.PlayerId} disconnected");
        }

        private void RemoveClientFromRoom(ClientHandler handler) {
            if (handler.RoomId == null) {
                return;
            }

            GameRoom? room = null;
            lock (_lock) {
                if (!_rooms.TryGetValue(handler.RoomId, out room)) {
                    return;
                }
            }

            bool closeRoom = false;
            lock (_roomLocks[handler.RoomId]) {
                room.PlayerIds.Remove(handler.PlayerId);

                if (room.HostPlayerId == handler.PlayerId) {
                    room.HostPlayerId = -1;
                    for (int i = 0; i < room.PlayerIds.Count; i++) {
                        if (!isBot(room.PlayerIds[i])) {
                            room.HostPlayerId = room.PlayerIds[i];
                            break;
                        }
                    }

                    if (room.HostPlayerId != -1) {
                        BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.NewHost, new() {
                                { "hostId", room.HostPlayerId.ToString() }
                            }));
                    } else {
                        closeRoom = true;
                        if (room.BombThread != null) {
                            if (room.BombThread.IsAlive) {
                                room.BombCts?.Cancel();
                                room.BombThread.Join();
                            }
                        }
                    }
                }

                if (room.GameStarted) {
                    room.Map.PlayerPositions.Remove(handler.PlayerId);
                    BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.PlayerLeft, new() {
                        { "playerId", handler.PlayerId.ToString() }
                    }));
                } else {
                    BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.PlayerLeft, new() {
                        { "playerId", handler.PlayerId.ToString() }
                    }));
                }
            }

            if (closeRoom) {
                foreach (var playerId in room.PlayerIds) {
                    if (isBot(playerId)) {
                        GameBot? bot = _bots.Find(b => b.BotId == playerId);
                        if (bot == null) continue;
                        bot.Dispose();
                        lock (_lock) {
                            _bots.Remove(bot);
                        }
                    }
                }

                lock (_lock) {
                    _rooms.Remove(room.RoomId);
                    _roomLocks.Remove(room.RoomId);
                }
            }

            handler.RoomId = null;
        }

        private void BroadcastRoomListToLobby() {
            var lobbyClients = _clients.Where(c => c.RoomId == null).ToList();
            foreach (var client in lobbyClients) {
                SendRoomListToClient(client.PlayerId);
            }
        }

        private void ProcessClientMessage(int playerId, NetworkMessage message) {
            ClientHandler? client = null;
            string? roomId = null;
            lock (_lock) {
                if (!isBot(playerId)) {
                    client = _clients.Find(c => c.PlayerId == playerId);
                    if (client != null) {
                        roomId = client.RoomId;
                    }
                } else {
                    roomId = _bots.Find(b => b.BotId == playerId)?.RoomId;
                }
            }

            switch (Enum.Parse<ClientMessageType>(message.Type.Name)) {
                case ClientMessageType.Ping: {
                        SendToClient(playerId, NetworkMessage.From(ServerMessageType.Pong));
                    }
                    break;
                case ClientMessageType.GetClientId: {
                        SendToClient(playerId, NetworkMessage.From(ServerMessageType.ClientId, new() {
                            { "clientId", playerId.ToString() }
                        }));
                    }
                    break;
                case ClientMessageType.CreateRoom: {
                        if (roomId != null) return;

                        string newRoomId = GenerateRoomId();

                        lock (_lock) {
                            GameRoom newRoom = new GameRoom(newRoomId, playerId, GenerateRandomMap());
                            _rooms.Add(newRoomId, newRoom);
                            _roomLocks.Add(newRoomId, new ReaderWriterLockSlim());

                            client!.RoomId = newRoomId;

                            SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomCreated));
                        }
                    }
                    break;
                case ClientMessageType.GetRoomInfo: {
                        lock (_roomLocks[roomId!]) {
                            if (_rooms.TryGetValue(roomId!, out GameRoom? room)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomInfo, new() {
                                    { "roomId", roomId! },
                                    { "hostId", room.HostPlayerId.ToString() },
                                    { "isHost", (playerId == room.HostPlayerId).ToString().ToLower() },
                                    { "playerIds", string.Join(";", room.PlayerIds) },
                                }));
                            }
                        }
                    }
                    break;
                case ClientMessageType.AddBot: {
                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room)) return;

                            if (room.HostPlayerId != playerId) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Only the host can add bots" }
                                }));
                                return;
                            }

                            if (room.PlayerIds.Count >= MAX_PLAYERS_PER_ROOM) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Room is full" }
                                }));
                                return;
                            }

                            int botId = GenerateBotId();
                            _bots.Add(new GameBot(botId, roomId!, ProcessClientMessage));
                            room.PlayerIds.Add(botId);

                            foreach (var existingPlayerId in room.PlayerIds) {
                                if (existingPlayerId != botId) {
                                    SendToClient(existingPlayerId, NetworkMessage.From(ServerMessageType.PlayerJoined, new() {
                                        { "playerId", botId.ToString() },
                                    }));
                                }
                            }
                        }
                    }
                    break;
                case ClientMessageType.JoinRoom: {
                        if (roomId != null) return; // Already in a room

                        string joinRoomId = message.Data["roomId"];

                        lock (_lock) {
                            if (!_rooms.ContainsKey(joinRoomId)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Room not found" }
                                }));
                                return;
                            }
                        }

                        lock (_roomLocks[joinRoomId]) {
                            _rooms.TryGetValue(joinRoomId, out GameRoom? room);
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
                            client!.RoomId = joinRoomId;

                            // Notify the player that they joined the room
                            SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomJoined));

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
                    }
                    break;
                case ClientMessageType.LeaveRoom: {
                        lock (_roomLocks[roomId!]) {
                            RemoveClientFromRoom(client!);
                        }
                    }
                    break;
                case ClientMessageType.StartGame: {
                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room)) return;

                            // Only the host can start the game
                            if (room.HostPlayerId != playerId) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Only the host can start the game" }
                                }));
                                return;
                            }

                            // Start the game
                            room.BombCts = new CancellationTokenSource();
                            room.BombThread = new Thread(() => ProcessBombs(roomId!)) {
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
                            BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.GameStarted));

                            // Update room list for all clients in lobby since this room is no longer joinable
                            // BroadcastRoomListToLobby();
                        }
                    }
                    break;
                case ClientMessageType.GetGameInfo: {
                        lock (_roomLocks[roomId!]) {
                            if (_rooms.TryGetValue(roomId!, out GameRoom? room)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.GameInfo, new() {
                                    { "map", room.Map.ToString() },
                                    { "playerCount", room.PlayerIds.Count.ToString() },
                                    { "playerIds", string.Join(";", room.PlayerIds) },
                                    { "playerPositions", string.Join(";", room.Map.PlayerPositions.Select(p => new Position(p.Value.X, p.Value.Y).ToString())) },
                                }));
                            }
                        }
                    }
                    break;
                case ClientMessageType.MovePlayer: {
                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room) || !room.GameStarted) return;

                            Direction direction = Enum.Parse<Direction>(message.Data["direction"]);
                            room.Map.MovePlayer(playerId, direction);

                            BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.PlayerMoved, new() {
                                { "playerId", playerId.ToString() },
                                { "x", room.Map.GetPlayerPosition(playerId).X.ToString() },
                                { "y", room.Map.GetPlayerPosition(playerId).Y.ToString() },
                                { "d", direction.ToString() }
                            }));
                        }
                    }
                    break;
                case ClientMessageType.PlaceBomb: {
                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room) || !room.GameStarted) return;

                            int x = int.Parse(message.Data["x"]);
                            int y = int.Parse(message.Data["y"]);
                            BombType type = Enum.Parse<BombType>(message.Data["type"]);

                            if (!room.Map.HasBomb(x, y)) {
                                room.Map.AddBomb(x, y, type, playerId);

                                BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.BombPlaced, new() {
                                    { "x", x.ToString() },
                                    { "y", y.ToString() },
                                    { "type", type.ToString() }
                                }));
                            }
                        }
                    }
                    break;
                case ClientMessageType.RefreshRooms: {
                        SendRoomListToClient(playerId);
                    }
                    break;
            }
        }

        private void BroadcastToRoom(string roomId, NetworkMessage message) {
            byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");

            lock (_lock) {
                if (!_rooms.TryGetValue(roomId, out GameRoom? room)) return;

                foreach (var playerId in room.PlayerIds) {
                    if (isBot(playerId)) {
                        _bots.Find(b => b.BotId == playerId)?.HandleResponse(message);
                    } else {
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
                if (isBot(playerId)) {
                    _bots.Find(b => b.BotId == playerId)?.HandleResponse(message);
                } else {
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
}
