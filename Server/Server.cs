using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Shared;

namespace Server {
    public class Server {
        private TcpListener? _server;
        private List<ClientHandler> _clients = new List<ClientHandler>();
        private int _nextPlayerId = 0;
        private Map _map = null!;
        private readonly object _lock = new object();

        public class ClientHandler {
            public int PlayerId { get; set; }
            public required TcpClient Client { get; set; }
            public required NetworkStream Stream { get; set; }
        }

        public void Start() {
            generateRandomMap();

            _server = new TcpListener(IPAddress.Any, 5000);
            _server.Start();
            Console.WriteLine("Server is running on port 5000...");

            Thread bombThread = new Thread(ProcessBombs);
            bombThread.IsBackground = true;
            bombThread.Start();

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
                        Random rand = new Random();
                        Position initialPosition = GenerateInitialPosition();
                        _map.SetPlayerPosition(playerId, initialPosition.X, initialPosition.Y);
                    }

                    Console.WriteLine($"Client connected: Player {playerId}");

                    SendToClient(playerId, new NetworkMessage(MessageType.InitMap, new Dictionary<string, string> {
                        { "playerId", playerId.ToString() },
                        { "map", _map.ToString() },
                    }));

                    foreach (var player in _map.PlayerPositions) {
                        if (player.Key != playerId) {
                            SendToClient(playerId, new NetworkMessage(MessageType.InitPlayer, new Dictionary<string, string> {
                                { "playerId", player.Key.ToString() },
                                { "skinId", (player.Key % Enum.GetNames<PlayerSkin>().Length).ToString() },
                                { "x", _map.GetPlayerPosition(player.Key).X.ToString() },
                                { "y", _map.GetPlayerPosition(player.Key).Y.ToString() },
                                { "d", Direction.Down.ToString() }
                            }));
                        }
                    }

                    BroadcastToAll(new NetworkMessage(MessageType.InitPlayer, new Dictionary<string, string> {
                        { "playerId", playerId.ToString() },
                        { "skinId", (playerId % Enum.GetNames<PlayerSkin>().Length).ToString() },
                        { "x", _map.GetPlayerPosition(playerId).X.ToString() },
                        { "y", _map.GetPlayerPosition(playerId).Y.ToString() },
                        { "d", Direction.Down.ToString() }
                    }));

                    Thread clientThread = new Thread(HandleClient);
                    clientThread.IsBackground = true;
                    clientThread.Start(clientHandler);
                } catch (Exception ex) {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private void generateRandomMap() {
            int height = 19;
            int width = 19;
            _map = new Map(height, width);

            if (Utils.RandomInt(0, 2) < 0) {
                for (int i = 0; i < height; i++) {
                    _map.SetTile(i, 0, TileType.Wall);
                    _map.SetTile(i, width - 1, TileType.Wall);
                }

                for (int j = 1; j < width - 1; j++) {
                    _map.SetTile(0, j, TileType.Wall);
                    _map.SetTile(height - 1, j, TileType.Wall);
                }

                for (int i = 0; i < 100; i++) {
                    int x = Utils.RandomInt(1, height - 1);
                    int y = Utils.RandomInt(1, width - 1);
                    _map.SetTile(x, y, TileType.Wall);
                }
            } else {
                for (int i = 0; i < height; i++) {
                    for (int j = 0; j < width; j++) {
                        _map.SetTile(i, j, TileType.Wall);
                    }
                }

                bool[,] visited = new bool[height, width];
                List<(int x, int y, int fromX, int fromY)> walls = [];

                int startX = Utils.RandomInt(height / 2) * 2 + 1;
                int startY = Utils.RandomInt(width / 2) * 2 + 1;

                visited[startX, startY] = true;
                _map.SetTile(startX, startY, TileType.Empty);

                foreach (var (dx, dy) in new[] { (2, 0), (-2, 0), (0, 2), (0, -2) }) {
                    int nx = startX + dx;
                    int ny = startY + dy;
                    if (_map.IsInBounds(nx, ny)) {
                        walls.Add((nx, ny, startX, startY));
                    }
                }

                while (walls.Count > 0) {
                    int index = Utils.RandomInt(walls.Count);
                    var (x, y, fromX, fromY) = walls[index];
                    walls.RemoveAt(index);

                    if (!_map.IsInBounds(x, y) || visited[x, y]) continue;

                    visited[x, y] = true;
                    _map.SetTile(x, y, TileType.Empty);

                    int wallX = (x + fromX) / 2;
                    int wallY = (y + fromY) / 2;
                    _map.SetTile(wallX, wallY, TileType.Empty);

                    foreach (var (dx, dy) in new[] { (2, 0), (-2, 0), (0, 2), (0, -2) }) {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (_map.IsInBounds(nx, ny) && !visited[nx, ny]) {
                            walls.Add((nx, ny, x, y));
                        }
                    }
                }
            }
        }

        private void ProcessBombs() {
            while (true) {
                lock (_lock) {
                    try {
                        List<Bomb> explodedBombs = [];
                        var bombsSnapshot = _map.Bombs.ToList();
                        foreach (var bomb in bombsSnapshot) {
                            if ((DateTime.Now - bomb.PlaceTime).TotalSeconds >= 2.0) {
                                explodedBombs.Add(bomb);
                            }
                        }

                        foreach (var bomb in explodedBombs) {
                            _map.ExplodeBomb(bomb.Position.X, bomb.Position.Y);
                            BroadcastToAll(new NetworkMessage(MessageType.ExplodeBomb, new Dictionary<string, string> {
                                { "x", bomb.Position.X.ToString() },
                                { "y", bomb.Position.Y.ToString() },
                                { "positions", string.Join(";", bomb.ExplosionPositions) }
                            }));

                            foreach (var pos in bomb.ExplosionPositions) {
                                CheckForPlayersInExplosion(pos.X, pos.Y);
                            }

                            _map.Bombs.Remove(bomb);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Error processing bombs: {ex.Message}");
                    }
                }

                Thread.Sleep(100);
            }
        }

        private void CheckForPlayersInExplosion(int x, int y) {
            foreach (var player in _map.PlayerPositions) {
                Position playerPos = player.Value;
                if (playerPos.X == x && playerPos.Y == y) {
                    Position respawnPosition = GenerateInitialPosition();
                    _map.SetPlayerPosition(player.Key, respawnPosition.X, respawnPosition.Y);
                    BroadcastToAll(new NetworkMessage(MessageType.RespawnPlayer, new Dictionary<string, string> {
                        { "playerId", player.Key.ToString() },
                        { "x", respawnPosition.X.ToString() },
                        { "y", respawnPosition.Y.ToString() },
                        { "d", Direction.Down.ToString() }
                    }));
                }
            }
        }

        private Position GenerateInitialPosition() {
            int x, y;
            do {
                x = Utils.RandomInt(0, _map.Height);
                y = Utils.RandomInt(0, _map.Width);
            } while (_map.GetTile(x, y) != TileType.Empty);

            return new Position(x, y);
        }

        private void HandleClient(object? obj) {
            if (obj is not ClientHandler handler)
                throw new ArgumentNullException(nameof(obj), "Expected a non-null ClientHandler object.");
            NetworkStream stream = handler.Stream;
            byte[] buffer = new byte[1024];

            try {
                while (true) {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] messages = message.Split(['\n', '\r', '|'], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var msg in messages) {
                        if (!string.IsNullOrEmpty(msg)) {
                            ProcessClientMessage(handler.PlayerId, NetworkMessage.FromJson(msg));
                        }
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error handling client {handler.PlayerId}: {ex.Message}");
            } finally {
                lock (_lock) {
                    _clients.Remove(handler);
                    _map.PlayerPositions.Remove(handler.PlayerId);
                }

                Console.WriteLine($"Player {handler.PlayerId} disconnected");
                BroadcastToAll(new NetworkMessage(MessageType.RemovePlayer, new Dictionary<string, string> {
                    { "playerId", handler.PlayerId.ToString() }
                }));

                try {
                    handler.Stream.Close();
                    handler.Client.Close();
                } catch { }
            }
        }

        private void ProcessClientMessage(int playerId, NetworkMessage message) {
            switch (message.Type) {
                case MessageType.MovePlayer: {
                        Direction direction = Enum.Parse<Direction>(message.Data["direction"]);
                        _map.MovePlayer(playerId, direction);
                        BroadcastToAll(new NetworkMessage(MessageType.MovePlayer, new Dictionary<string, string> {
                            { "playerId", playerId.ToString() },
                            { "x", _map.GetPlayerPosition(playerId).X.ToString() },
                            { "y", _map.GetPlayerPosition(playerId).Y.ToString() },
                            { "d", direction.ToString() }
                        }));
                    }
                    break;
                case MessageType.PlaceBomb: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        BombType type = Enum.Parse<BombType>(message.Data["type"]);
                        if (!_map.HasBomb(x, y)) {
                            _map.AddBomb(x, y, type);
                            BroadcastToAll(new NetworkMessage(MessageType.PlaceBomb, new Dictionary<string, string> {
                                { "x", x.ToString() },
                                { "y", y.ToString() },
                                { "type", type.ToString() }
                            }));
                        }
                    }
                    break;
            }
        }

        private void BroadcastToAll(NetworkMessage message) {
            byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");

            lock (_lock) {
                foreach (var client in _clients) {
                    try {
                        client.Stream.Write(data, 0, data.Length);
                    } catch {

                    }
                }
            }
        }

        private void SendToClient(int playerId, NetworkMessage message) {
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
