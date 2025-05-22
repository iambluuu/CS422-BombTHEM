using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Server.PowerHandler;

using Shared;

namespace Server {
    public class Server {
        private TcpListener? _server;
        private readonly List<PlayerHandler> _players = [];
        private static readonly Dictionary<int, PlayerHandler> _idToPlayer = [];
        private readonly Dictionary<string, GameRoom> _rooms = [];
        private readonly object _lock = new();
        private readonly Dictionary<string, object> _roomLocks = [];
        private const int MAX_PLAYERS_PER_ROOM = 4;

        public abstract class PlayerHandler {
            public required int PlayerId { get; set; }
            public required string Username { get; set; }
            public string? RoomId { get; set; }
            public bool InGame { get; set; } = false;

            public virtual void Dispose() { }
        }

        public class ClientHandler : PlayerHandler {
            public required TcpClient Client { get; set; }
            public required NetworkStream Stream { get; set; }
            public Thread Thread { get; set; } = null!;
            public CancellationTokenSource Cts { get; set; } = null!;
            public bool Connected { get; set; } = true;
            public System.Timers.Timer? AliveTimer { get; set; } = null;

            public override void Dispose() {
                try {
                    Cts.Cancel();
                    Thread.Join();
                    Stream.Close();
                    Client.Close();
                    AliveTimer?.Stop();
                    AliveTimer?.Dispose();
                    Connected = false;
                } catch (Exception ex) {
                    Console.WriteLine($"Error disposing client {PlayerId}: {ex.Message}");
                }

                Console.WriteLine($"Client {PlayerId} disposed");
            }
        }

        public class BotHandler : PlayerHandler {
            public required GameBot Bot { get; set; }

            public override void Dispose() {
                try {
                    Bot.Dispose();
                } catch (Exception ex) {
                    Console.WriteLine($"Error disposing bot {PlayerId}: {ex.Message}");
                }

                Console.WriteLine($"Bot {PlayerId} disposed");
            }
        }

        public class GameRoom {
            public string RoomId { get; set; }
            public int HostPlayerId { get; set; }
            public List<int> PlayerIds { get; set; } = [];
            public Map Map { get; set; } = null!;
            public Dictionary<int, Position> InitialPositions { get; set; } = [];
            public Dictionary<int, int> PlayerScores { get; set; } = [];
            public Dictionary<int, DateTime> PlayerLastDied { get; set; } = [];
            public bool GameStarted { get; set; } = false;
            public Thread? BombThread { get; set; } = null;
            public CancellationTokenSource? BombCts { get; set; } = null;
            public Thread? ItemThread { get; set; } = null;
            public CancellationTokenSource? ItemCts { get; set; } = null;
            public Thread? PowerUpThread { get; set; } = null;
            public CancellationTokenSource? PowerUpCts { get; set; } = null;
            public System.Timers.Timer? GameTimer { get; set; } = null;
            public bool Closed { get; set; } = false;

            public GameRoom(string roomId, int hostPlayerId) {
                RoomId = roomId;
                HostPlayerId = hostPlayerId;
                PlayerIds.Add(hostPlayerId);
                PlayerScores.Add(hostPlayerId, 0);
            }

            public void ClearData() {
                Map = null!;
                InitialPositions.Clear();
                PlayerScores.Clear();
                PlayerLastDied.Clear();
                GameStarted = false;
                BombThread = null;
                BombCts = null;
                ItemThread = null;
                ItemCts = null;
                PowerUpThread = null;
                PowerUpCts = null;
                GameTimer = null;
            }

            public void AddPlayer(int playerId) {
                PlayerIds.Add(playerId);
            }

            public void RemovePlayer(int playerId) {
                PlayerIds.Remove(playerId);
                if (!GameStarted) {
                    Map?.PlayerInfos.Remove(playerId);
                }
            }

            public void StopGame() {
                BombCts?.Cancel();
                ItemCts?.Cancel();
                PowerUpCts?.Cancel();
                GameTimer?.Stop();
                GameTimer?.Dispose();
            }

            public void Dispose() {
                StopGame();
                Closed = true;
                Console.WriteLine($"Room {RoomId} disposed");
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
                        Username = "",
                        Client = client,
                        Stream = client.GetStream()
                    };

                    lock (_lock) {
                        _players.Add(clientHandler);
                        _idToPlayer.Add(playerId, clientHandler);
                    }

                    Console.WriteLine($"Client {playerId} connected");

                    clientHandler.Cts = new CancellationTokenSource();
                    clientHandler.Thread = new(() => HandleClient(clientHandler)) {
                        IsBackground = true
                    };
                    clientHandler.Thread.Start();

                    clientHandler.AliveTimer = new System.Timers.Timer(10000) {
                        Enabled = true,
                        AutoReset = false
                    };
                    clientHandler.AliveTimer.Elapsed += (sender, e) => {
                        Console.WriteLine($"Client {playerId} is inactive, disconnecting...");
                        DisconnectPlayer(clientHandler);
                    };
                    clientHandler.AliveTimer.Start();
                } catch (Exception ex) {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private bool IsBot(int playerId) {
            return playerId < 100000;
        }

        private PlayerHandler? GetPlayerById(int playerId) {
            lock (_lock) {
                if (_idToPlayer.TryGetValue(playerId, out PlayerHandler? client)) {
                    return client;
                } else {
                    return null;
                }
            }
        }

        private int GeneratePlayerId() {
            while (true) {
                int playerId = Utils.RandomInt(100000, 1000000);
                lock (_lock) {
                    if (!_idToPlayer.ContainsKey(playerId)) {
                        return playerId;
                    }
                }
            }
        }

        private int GenerateBotId() {
            while (true) {
                int botId = Utils.RandomInt(100000);
                lock (_lock) {
                    if (!_idToPlayer.ContainsKey(botId)) {
                        return botId;
                    }
                }
            }
        }

        private string GenerateRoomId() {
            while (true) {
                string roomId = string.Empty;
                for (int i = 0; i < 6; i++) {
                    roomId += (char)(Utils.RandomInt(26) + 'A');
                }

                lock (_lock) {
                    if (!_rooms.ContainsKey(roomId)) {
                        return roomId;
                    }
                }
            }
        }

        private Map GenerateRandomMap() {
            const int height = 15;
            const int width = 15;

            Map map = new Map(height, width);
            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    if (i == 0 || i == height - 1 || j == 0 || j == width - 1 || (i % 2 == 0 && j % 2 == 0)) {
                        map.SetTile(i, j, TileType.Wall);
                    }
                }
            }

            // bool[,] visited = new bool[height, width];
            // List<(int x, int y, int fromX, int fromY)> walls = [];

            // int startX = Utils.RandomInt(height / 2) * 2 + 1;
            // int startY = Utils.RandomInt(width / 2) * 2 + 1;

            // visited[startX, startY] = true;
            // map.SetTile(startX, startY, TileType.Empty);

            // foreach (var (dx, dy) in new[] { (2, 0), (-2, 0), (0, 2), (0, -2) }) {
            //     int nx = startX + dx;
            //     int ny = startY + dy;
            //     if (map.IsInBounds(nx, ny)) {
            //         walls.Add((nx, ny, startX, startY));
            //     }
            // }

            // while (walls.Count > 0) {
            //     int index = Utils.RandomInt(walls.Count);
            //     var (x, y, fromX, fromY) = walls[index];
            //     walls.RemoveAt(index);

            //     if (!map.IsInBounds(x, y) || visited[x, y]) continue;

            //     visited[x, y] = true;
            //     map.SetTile(x, y, TileType.Empty);

            //     int wallX = (x + fromX) / 2;
            //     int wallY = (y + fromY) / 2;
            //     map.SetTile(wallX, wallY, TileType.Empty);

            //     foreach (var (dx, dy) in new[] { (2, 0), (-2, 0), (0, 2), (0, -2) }) {
            //         int nx = x + dx;
            //         int ny = y + dy;
            //         if (map.IsInBounds(nx, ny) && !visited[nx, ny]) {
            //             walls.Add((nx, ny, x, y));
            //         }
            //     }
            // }

            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    if (map.GetTile(i, j) == TileType.Empty) {
                        int Distance(int x1, int y1, int x2, int y2) {
                            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
                        }

                        int distance = Math.Min(Math.Min(Distance(i, j, 1, 1), Distance(i, j, height - 2, width - 2)), Math.Min(Distance(i, j, 1, width - 2), Distance(i, j, height - 2, 1)));

                        if (distance > 2) {
                            map.SetTile(i, j, TileType.Grass);
                        }
                    }
                }
            }

            return map;
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

                    handler.AliveTimer?.Stop();
                    handler.AliveTimer?.Start();

                    messageBuffer.Write(buffer, 0, bytesRead);
                    ProcessMessageBuffer(handler.PlayerId, messageBuffer);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error handling client {handler.PlayerId}: {ex.Message}");
            } finally {
                messageBuffer.Dispose();
                DisconnectPlayer(handler);
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
                        // Console.WriteLine($"Received message from client {playerId}: {messageString}");
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

        private void ProcessClientMessage(int playerId, NetworkMessage message) {
            PlayerHandler? client = GetPlayerById(playerId);
            if (client == null) {
                return;
            }

            string? roomId = client.RoomId;

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
                case ClientMessageType.GetUsername: {
                        SendToClient(playerId, NetworkMessage.From(ServerMessageType.UsernameSet, new() {
                            { "username", client.Username }
                        }));
                    }
                    break;
                case ClientMessageType.SetUsername: {
                        if (client == null) return;

                        string username = message.Data["username"];

                        lock (_lock) {
                            client.Username = username;
                        }

                        if (roomId != null) {
                            BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.UsernameSet, new() {
                                { "playerId", playerId.ToString() },
                                { "username", username }
                            }));
                        }
                    }
                    break;
                case ClientMessageType.CreateRoom: {
                        if (roomId != null) return;

                        string newRoomId = GenerateRoomId();

                        lock (_lock) {
                            GameRoom newRoom = new(newRoomId, playerId);
                            _rooms.Add(newRoomId, newRoom);
                            _roomLocks.Add(newRoomId, new object());

                            client!.RoomId = newRoomId;

                            SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomCreated));
                        }

                        Console.WriteLine($"Client {playerId} created room {newRoomId}");
                    }
                    break;
                case ClientMessageType.GetRoomInfo: {
                        if (roomId == null) return;

                        lock (_roomLocks[roomId!]) {
                            if (_rooms.TryGetValue(roomId!, out GameRoom? room)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomInfo, new() {
                                    { "roomId", roomId! },
                                    { "hostId", room.HostPlayerId.ToString() },
                                    { "isHost", (playerId == room.HostPlayerId).ToString().ToLower() },
                                    { "playerIds", string.Join(";", room.PlayerIds) },
                                    { "usernames", string.Join(";", room.PlayerIds.Select(id => _idToPlayer[id].Username)) },
                                    { "inGames", string.Join(";", room.PlayerIds.Select(id => _idToPlayer[id].InGame.ToString() )) },
                                }));
                            }
                        }
                    }
                    break;
                case ClientMessageType.AddBot: {
                        int botId;
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

                            botId = GenerateBotId();
                            BotHandler bot = new() {
                                PlayerId = botId,
                                Username = "Bot" + botId,
                                RoomId = roomId!,
                                Bot = new GameBot(botId, roomId!, ProcessClientMessage)
                            };
                            _players.Add(bot);
                            _idToPlayer.Add(botId, bot);
                            room.AddPlayer(botId);

                            foreach (var existingPlayerId in room.PlayerIds) {
                                if (existingPlayerId != botId) {
                                    SendToClient(existingPlayerId, NetworkMessage.From(ServerMessageType.PlayerJoined, new() {
                                        { "playerId", botId.ToString() },
                                        { "username", bot.Username }
                                    }));
                                }
                            }
                        }

                        Console.WriteLine($"Client {playerId} added bot {botId} to room {roomId}");
                    }
                    break;
                case ClientMessageType.KickPlayer: {
                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room)) return;

                            if (room.HostPlayerId != playerId) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Only the host can kick players" }
                                }));
                                return;
                            }

                            int playerToKick = int.Parse(message.Data["playerId"]);

                            if (playerToKick == playerId) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "You cannot kick yourself" }
                                }));
                                return;
                            }

                            RemovePlayerFromRoom(_idToPlayer[playerToKick]);

                            if (!IsBot(playerToKick)) {
                                SendToClient(playerToKick, NetworkMessage.From(ServerMessageType.PlayerKicked, new() {
                                    { "message", "You have been kicked from the room" }
                                }));
                            }
                        }

                        Console.WriteLine($"Client {playerId} kicked player {message.Data["playerId"]} from room {roomId}");
                    }
                    break;
                case ClientMessageType.JoinRoom: {
                        if (roomId != null) return;

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

                            if (room.PlayerIds.Count >= MAX_PLAYERS_PER_ROOM) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Room is full" }
                                }));
                                return;
                            }

                            room.AddPlayer(playerId);
                            client!.RoomId = joinRoomId;

                            SendToClient(playerId, NetworkMessage.From(ServerMessageType.RoomJoined));
                            foreach (var existingPlayerId in room.PlayerIds) {
                                if (existingPlayerId != playerId) {
                                    SendToClient(existingPlayerId, NetworkMessage.From(ServerMessageType.PlayerJoined, new() {
                                        { "playerId", playerId.ToString() },
                                        { "username", client.Username }
                                    }));
                                }
                            }
                        }

                        Console.WriteLine($"Client {playerId} joined room {joinRoomId}");
                    }
                    break;
                case ClientMessageType.LeaveRoom: {
                        RemovePlayerFromRoom(client);
                    }
                    break;
                case ClientMessageType.StartGame: {
                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room)) return;

                            if (room.HostPlayerId != playerId) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.Error, new() {
                                    { "message", "Only the host can start the game" }
                                }));
                                return;
                            }

                            room.ClearData();

                            room.GameStarted = true;

                            room.Map = GenerateRandomMap();

                            room.BombCts = new CancellationTokenSource();
                            room.BombThread = new Thread(() => ProcessBombs(roomId!)) {
                                IsBackground = true
                            };
                            room.BombThread.Start();

                            room.ItemCts = new CancellationTokenSource();
                            room.ItemThread = new Thread(() => ProcessDroppedItems(roomId!)) {
                                IsBackground = true
                            };
                            room.ItemThread.Start();

                            room.PowerUpCts = new CancellationTokenSource();
                            room.PowerUpThread = new Thread(() => ProcessActivePowerUps(roomId!)) {
                                IsBackground = true
                            };
                            room.PowerUpThread.Start();

                            room.GameTimer = new System.Timers.Timer(GameplayConfig.GameDuration * 1000) {
                                Enabled = true
                            };
                            room.GameTimer.Elapsed += (sender, e) => {
                                StopGame(roomId!);
                            };
                            room.GameTimer.Start();

                            for (int i = 0; i < room.PlayerIds.Count; i++) {
                                int pid = room.PlayerIds[i];
                                _idToPlayer[pid].InGame = true;

                                Position initialPosition = new Position(0, 0);

                                if (i == 0 || i == 2) {
                                    initialPosition.X = 1;
                                } else {
                                    initialPosition.X = room.Map.Height - 2;
                                }

                                if (i == 0 || i == 3) {
                                    initialPosition.Y = 1;
                                } else {
                                    initialPosition.Y = room.Map.Width - 2;
                                }

                                room.InitialPositions.Add(pid, initialPosition);
                                room.Map.SetPlayerPosition(pid, initialPosition.X, initialPosition.Y);
                                room.PlayerScores.Add(pid, 0);
                            }

                            BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.GameStarted));
                        }
                    }
                    break;
                case ClientMessageType.LeaveGame: {
                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room)) return;

                            _idToPlayer[playerId].InGame = false;
                            BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.GameLeft, new() {
                                { "playerId", playerId.ToString() }
                            }));

                            bool allClientsLeft = true;
                            foreach (var pid in room.PlayerIds) {
                                if (!IsBot(pid) && _idToPlayer[pid].InGame) {
                                    allClientsLeft = false;
                                    break;
                                }
                            }

                            if (allClientsLeft) {
                                StopGame(roomId!);
                                foreach (var pid in room.PlayerIds) {
                                    if (IsBot(pid)) {
                                        BotHandler bot = (BotHandler)_idToPlayer[pid];
                                        bot.InGame = false;
                                        BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.GameLeft, new() {
                                            { "playerId", pid.ToString() }
                                        }));
                                    }
                                }
                            }
                        }
                    }
                    break;
                case ClientMessageType.GetGameInfo: {
                        lock (_roomLocks[roomId!]) {
                            if (_rooms.TryGetValue(roomId!, out GameRoom? room)) {
                                SendToClient(playerId, NetworkMessage.From(ServerMessageType.GameInfo, new() {
                                    { "map", room.Map.ToString() },
                                    { "duration", GameplayConfig.GameDuration.ToString() },
                                    { "playerCount", room.PlayerIds.Count.ToString() },
                                    { "playerIds", string.Join(";", room.PlayerIds) },
                                    { "usernames", string.Join(";", room.PlayerIds.Select(id => _idToPlayer[id].Username)) },
                                    { "playerPositions", string.Join(";", room.Map.PlayerInfos.Select(p => new Position(p.Value.Position.X, p.Value.Position.Y).ToString())) },
                                }));
                            }
                        }
                    }
                    break;
                case ClientMessageType.MovePlayer: {
                        Direction direction = Enum.Parse<Direction>(message.Data["direction"]);

                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room) || room.Closed) return;
                            room.Map.MovePlayer(playerId, direction);
                            Position playerPos = room.Map.GetPlayerPosition(playerId);

                            BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.PlayerMoved, new() {
                                { "playerId", playerId.ToString() },
                                { "x", playerPos.X.ToString() },
                                { "y", playerPos.Y.ToString() },
                                { "d", direction.ToString() }
                            }));

                            PowerName pickedItem = room.Map.PickUpItem(playerId, playerPos.X, playerPos.Y);
                            if (pickedItem != PowerName.None) {
                                BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.ItemPickedUp, new() {
                                    { "playerId", playerId.ToString() },
                                    { "x", playerPos.X.ToString() },
                                    { "y", playerPos.Y.ToString() },
                                    { "powerUpType", pickedItem.ToString() }
                                }));
                            }
                        }
                    }
                    break;
                case ClientMessageType.PlaceBomb: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);

                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room) || room.Closed) return;

                            if (!room.Map.HasBomb(x, y) && room.Map.GetTile(x, y) == TileType.Empty) {
                                var activeNuke = room.Map.PlayerInfos[playerId].TryGetActivePowerUp(PowerName.Nuke);
                                BombType type = activeNuke != null ? BombType.Nuke : Enum.Parse<BombType>(message.Data["type"]);

                                if (!room.Map.AddBomb(x, y, type, playerId)) return;

                                if (activeNuke != null) {
                                    var parameters = PowerUpHandlerFactory.CreatePowerUpHandler(PowerName.Nuke)?.Use(room.Map, playerId, null);
                                    if (parameters != null) {
                                        BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.PowerUpUsed, new() {
                                            { "powerUpType", PowerName.Nuke.ToString()},
                                            { "parameters", JsonSerializer.Serialize(parameters)},
                                            { "slotNum", activeNuke.SlotNum.ToString() },
                                        }));
                                        if (!room.Map.PlayerInfos[playerId].CanUsePowerUp(PowerName.Nuke, activeNuke.SlotNum)) {
                                            room.Map.PlayerInfos[playerId].ExpireActivePowerUp(PowerName.Nuke, activeNuke.SlotNum);
                                            BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.PowerUpExpired, new() {
                                                { "powerUpType", PowerName.Nuke.ToString()},
                                                { "parameters", JsonSerializer.Serialize(parameters)},
                                                { "slotNum", activeNuke.SlotNum.ToString() },
                                                { "playerId", playerId.ToString() }
                                            }));
                                        }
                                    }
                                }

                                BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.BombPlaced, new() {
                                    { "x", x.ToString() },
                                    { "y", y.ToString() },
                                    { "type", type.ToString() },
                                    { "byPlayerId", playerId.ToString() },
                                    { "isCounted", (!room.Map.PlayerInfos[playerId].HasPowerUp(PowerName.MoreBombs)).ToString() }
                                }));
                            } else {
                                BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.BombPlaced, new() {
                                    { "x", x.ToString() },
                                    { "y", y.ToString() },
                                    { "byPlayerId", playerId.ToString() },
                                    { "invalid", "True" }
                                }));
                            }
                        }
                    }
                    break;
                case ClientMessageType.GetGameResults: {
                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room)) return;

                            List<int> playerIds = room.PlayerScores.Keys.ToList();
                            playerIds.Sort((a, b) => room.PlayerScores[b].CompareTo(room.PlayerScores[a]));

                            BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.GameResults, new() {
                                { "playerIds", string.Join(";", playerIds) },
                                { "usernames", string.Join(";", playerIds.Select(id => _idToPlayer[id].Username)) },
                                { "scores", string.Join(";", playerIds.Select(id => room.PlayerScores[id].ToString())) },
                            }));
                        }
                    }
                    break;

                case ClientMessageType.UsePowerUp: {
                        PowerName powerUpType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
                        int slotNum = int.Parse(message.Data["slotNum"]);
                        Dictionary<string, object>? parameters = null;

                        if (message.Data.TryGetValue("parameters", out var param)) {
                            parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(param.ToString());
                        }

                        lock (_roomLocks[roomId!]) {
                            if (!_rooms.TryGetValue(roomId!, out GameRoom? room) || !room.Map.CanUsePowerUp(playerId, powerUpType, slotNum)) {
                                Console.WriteLine($"Client {playerId} tried to use power-up {powerUpType} in slot {slotNum}, but it was invalid.");
                                BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.PowerUpUsed, new() {
                                    { "slotNum", slotNum.ToString() },
                                    { "invalid", "True" }
                                }));
                                return;
                            }

                            var powerUpHandler = PowerUpHandlerFactory.CreatePowerUpHandler(powerUpType);
                            var responseParams = powerUpHandler?.Apply(room.Map, playerId, parameters, slotNum);
                            if (responseParams != null) {
                                // Console.WriteLine($"Client {playerId} used power-up: {powerUpType}");
                                // Console.WriteLine($"Need to change: {responseParams["needToChange"]}");
                                BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.PowerUpUsed, new() {
                                    { "powerUpType", powerUpType.ToString() },
                                    { "parameters", JsonSerializer.Serialize(responseParams) },
                                    { "slotNum", slotNum.ToString() },
                                }));
                            } else {
                                BroadcastToRoom(roomId!, NetworkMessage.From(ServerMessageType.PowerUpUsed, new() {
                                    { "slotNum", slotNum.ToString() },
                                    { "invalid", "True" }
                                }));
                            }
                        }
                    }
                    break;
            }
        }

        private void ProcessBombs(string roomId) {
            GameRoom? room;
            lock (_roomLocks[roomId]) {
                if (!_rooms.TryGetValue(roomId, out room) || !room.GameStarted) {
                    return;
                }
            }

            while (room.GameStarted && !room.Closed && !room.BombCts!.Token.IsCancellationRequested) {
                try {
                    lock (_roomLocks[roomId]) {
                        List<Bomb> explodedBombs = [];
                        foreach (var bomb in room.Map.Bombs) {
                            if ((DateTime.Now - bomb.PlaceTime).TotalMilliseconds >= GameplayConfig.BombDuration) {
                                explodedBombs.Add(bomb);
                            }
                        }

                        HashSet<Position> explosionPositions = new();
                        Random rand = new Random();
                        foreach (var bomb in explodedBombs) {
                            room.Map.ExplodeBomb(bomb.Position.X, bomb.Position.Y);
                            foreach (var pos in bomb.ExplosionPositions) {
                                if (room.Map.GetTile(pos.X, pos.Y) == TileType.Grass) {
                                    explosionPositions.Add(pos);
                                }
                            }

                            BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.BombExploded, new() {
                                { "x", bomb.Position.X.ToString() },
                                { "y", bomb.Position.Y.ToString() },
                                { "positions", string.Join(";", bomb.ExplosionPositions) },
                                { "byPlayerId", bomb.PlayerId.ToString() },
                                { "isCounted", bomb.IsCounted.ToString() }
                            }));

                            foreach (var pos in bomb.ExplosionPositions) {
                                CheckForPlayersInExplosion(room, pos.X, pos.Y, bomb.PlayerId);
                            }

                            room.Map.RemoveBomb(bomb.Position.X, bomb.Position.Y);
                        }

                        foreach (var pos in explosionPositions) {
                            room.Map.SetTile(pos.X, pos.Y, TileType.Empty);
                            if (rand.NextDouble() < GameplayConfig.PowerUpSpawnChance) {
                                PowerName powerUpType = (PowerName)rand.Next(1, Enum.GetValues(typeof(PowerName)).Length);
                                // PowerName powerUpType = rand.Next(0, 2) == 0 ? (PowerName)Utils.RandomInt(2, Enum.GetValues<PowerName>().Length) : PowerName.MoreBombs;
                                room.Map.AddItem(pos.X, pos.Y, powerUpType);
                                BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.ItemSpawned, new() {
                                    { "x", pos.X.ToString() },
                                    { "y", pos.Y.ToString() },
                                    { "powerUpType", powerUpType.ToString() }
                                }));
                            }
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing bombs for room {roomId}: {ex.Message}");
                }

                Thread.Sleep(100);
            }
        }

        private void ProcessDroppedItems(string roomId) {
            GameRoom? room;
            lock (_roomLocks[roomId]) {
                if (!_rooms.TryGetValue(roomId, out room) || !room.GameStarted) {
                    return;
                }
            }

            while (room.GameStarted && !room.Closed && !room.ItemCts!.Token.IsCancellationRequested) {
                try {
                    lock (_roomLocks[roomId]) {
                        List<DroppedItem> expiredItems = [];
                        foreach (var item in room.Map.Items) {
                            if ((DateTime.Now - item.DropTime).TotalMilliseconds >= GameplayConfig.ItemExistDuration) {
                                expiredItems.Add(item);
                                BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.ItemExpired, new() {
                                    { "x", item.Position.X.ToString() },
                                    { "y", item.Position.Y.ToString() }
                                }));
                            }
                        }

                        foreach (var item in expiredItems) {
                            room.Map.RemoveItem(item.Position.X, item.Position.Y);
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing dropped items for room {roomId}: {ex.Message}");
                }

                Thread.Sleep(100);
            }
        }

        private void ProcessActivePowerUps(string roomId) {
            GameRoom? room;
            lock (_roomLocks[roomId]) {
                if (!_rooms.TryGetValue(roomId, out room) || !room.GameStarted) {
                    return;
                }
            }

            while (room.GameStarted && !room.Closed && !room.PowerUpCts!.Token.IsCancellationRequested) {
                try {
                    lock (_roomLocks[roomId]) {
                        List<(int playerId, PowerName powerType, int slotNum)> expiredPowerUps = [];
                        foreach (var player in room.Map.PlayerInfos) {
                            int playerId = player.Key;
                            PlayerIngameInfo playerInfo = player.Value;
                            foreach (var powerUp in playerInfo.ActivePowerUps) {
                                if (!GameplayConfig.PowerUpDurations.TryGetValue(powerUp.PowerType, out float value) || value < 0) {
                                    continue;
                                }

                                if ((DateTime.Now - powerUp.StartTime).TotalMilliseconds >= GameplayConfig.PowerUpDurations[powerUp.PowerType]) {
                                    expiredPowerUps.Add((playerId, powerUp.PowerType, powerUp.SlotNum));
                                    BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.PowerUpExpired, new() {
                                        { "playerId", playerId.ToString() },
                                        { "slotNum", powerUp.SlotNum.ToString()},
                                        { "powerUpType", powerUp.PowerType.ToString() }
                                    }));
                                }
                            }

                            foreach (var powerUp in expiredPowerUps) {
                                room.Map.PlayerInfos[powerUp.playerId].ExpireActivePowerUp(powerUp.powerType);
                            }
                        }

                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing active power-ups for room {roomId}: {ex.Message}");
                }

                Thread.Sleep(100);
            }
        }

        private void CheckForPlayersInExplosion(GameRoom room, int x, int y, int bombPlayerId) {
            foreach (var player in room.Map.PlayerInfos) {
                Position playerPos = player.Value.Position;
                if (bombPlayerId == player.Key || player.Value.HasPowerUp(PowerName.Shield)) {
                    continue;
                }

                if (playerPos.X == x && playerPos.Y == y && (!room.PlayerLastDied.ContainsKey(player.Key) || (DateTime.Now - room.PlayerLastDied[player.Key]).TotalMilliseconds >= 2000)) {
                    Position respawnPosition = room.InitialPositions[player.Key];
                    room.Map.SetPlayerPosition(player.Key, respawnPosition.X, respawnPosition.Y);
                    room.PlayerScores[bombPlayerId] += 1;
                    room.PlayerLastDied[player.Key] = DateTime.Now;

                    BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.PlayerDied, new() {
                        { "playerId", player.Key.ToString() },
                        { "byPlayerId", bombPlayerId.ToString() },
                        { "x", respawnPosition.X.ToString() },
                        { "y", respawnPosition.Y.ToString() },
                    }));
                }
            }
        }

        private void StopGame(string roomId) {
            _rooms.TryGetValue(roomId, out GameRoom? room);
            if (room == null) {
                return;
            }

            if (room.GameStarted == false) {
                return;
            }

            foreach (var playerId in room.PlayerIds) {
                if (IsBot(playerId)) {
                    BotHandler bot = (BotHandler)_idToPlayer[playerId];
                    bot.Bot.Stop();
                }
            }

            room.StopGame();
            room.GameStarted = false;

            BroadcastToRoom(roomId, NetworkMessage.From(ServerMessageType.GameStopped));
        }

        private void BroadcastToRoom(string roomId, NetworkMessage message) {
            lock (_lock) {
                if (!_rooms.TryGetValue(roomId, out GameRoom? room)) return;
                foreach (var playerId in room.PlayerIds) {
                    SendToClient(playerId, message);
                }
            }
        }

        // private void BroadcastToAll(NetworkMessage message) {
        //     byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");

        //     lock (_lock) {
        //         foreach (var client in _players) {
        //             try {
        //                 client.Stream.Write(data, 0, data.Length);
        //             } catch {
        //                 // Ignore errors
        //             }
        //         }
        //     }
        // }

        private void SendToClient(int playerId, NetworkMessage message) {
            if (message.Type.Direction != MessageDirection.Server) {
                Console.WriteLine("The sent message must be from the server side");
                return;
            }

            lock (_lock) {
                if (IsBot(playerId)) {
                    ((BotHandler)_idToPlayer[playerId]).Bot.HandleResponse(message);
                } else {
                    ClientHandler client = (ClientHandler)_idToPlayer[playerId];
                    try {
                        byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");
                        client.Stream.Write(data, 0, data.Length);
                    } catch (Exception ex) {
                        Console.WriteLine($"Error sending to client {client.PlayerId}: {ex.Message}");
                    }
                }
            }
        }

        private void DisconnectPlayer(ClientHandler handler) {
            RemovePlayerFromRoom(handler);

            if (handler == null || handler.Connected == false) {
                return;
            }

            handler.Dispose();
            lock (_lock) {
                _players.Remove(_idToPlayer[handler.PlayerId]);
                _idToPlayer.Remove(handler.PlayerId);
            }

            Console.WriteLine($"Client {handler.PlayerId} disconnected");
        }

        private void RemovePlayerFromRoom(PlayerHandler handler) {
            Console.WriteLine($"Removing player {handler.PlayerId} from room");

            if (handler == null || handler.RoomId == null) {
                return;
            }

            _rooms.TryGetValue(handler.RoomId, out GameRoom? room);
            lock (_roomLocks[handler.RoomId]) {
                room!.RemovePlayer(handler.PlayerId);

                if (room.HostPlayerId == handler.PlayerId) {
                    room.HostPlayerId = -1;
                    for (int i = 0; i < room.PlayerIds.Count; i++) {
                        if (!IsBot(room.PlayerIds[i])) {
                            room.HostPlayerId = room.PlayerIds[i];
                            break;
                        }
                    }

                    if (room.HostPlayerId != -1) {
                        BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.NewHost, new() {
                            { "hostId", room.HostPlayerId.ToString() }
                        }));
                    } else {
                        room.Closed = true;
                    }
                }

                BroadcastToRoom(room.RoomId, NetworkMessage.From(ServerMessageType.PlayerLeft, new() {
                    { "playerId", handler.PlayerId.ToString() }
                }));
            }

            if (room.Closed) {
                room.Dispose();
                foreach (var playerId in room.PlayerIds) {
                    if (IsBot(playerId)) {
                        lock (_lock) {
                            BotHandler bot = (BotHandler)_idToPlayer[playerId];
                            bot.Dispose();
                            _players.Remove(bot);
                            _idToPlayer.Remove(playerId);
                        }
                    }
                }

                lock (_lock) {
                    _rooms.Remove(room.RoomId);
                    _roomLocks.Remove(room.RoomId);
                }

                Console.WriteLine($"Room {room.RoomId} closed");
            } else {
                if (IsBot(handler.PlayerId)) {
                    handler.Dispose();
                }
            }

            Console.WriteLine($"Player {handler.PlayerId} left room {handler.RoomId}");

            handler.RoomId = null;
        }
    }
}
