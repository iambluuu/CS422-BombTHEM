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

                    SendToClient(playerId, new NetworkMessage(MessageType.InitPlayer, new Dictionary<string, string> {
                        { "playerId", playerId.ToString() },
                        { "map", _map.ToString() },
                    }));

                    foreach (var player in _map.PlayerPositions) {
                        if (player.Key != playerId) {
                            SendToClient(playerId, new NetworkMessage(MessageType.MovePlayer, new Dictionary<string, string> {
                                { "playerId", player.Key.ToString() },
                                { "x", _map.GetPlayerPosition(player.Key).X.ToString() },
                                { "y", _map.GetPlayerPosition(player.Key).Y.ToString() }
                            }));
                        }
                    }

                    BroadcastToAll(new NetworkMessage(MessageType.MovePlayer, new Dictionary<string, string> {
                        { "playerId", playerId.ToString() },
                        { "x", _map.GetPlayerPosition(playerId).X.ToString() },
                        { "y", _map.GetPlayerPosition(playerId).Y.ToString() }
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
            Random rand = new Random();
            _map = new Map(20, 15);
            for (int i = 100; i > 0; i--) {
                int x = rand.Next(0, _map.Height);
                int y = rand.Next(0, _map.Width);
                _map.SetTile(x, y, TileType.Wall);
            }
        }

        private void ProcessBombs() {
            while (true) {
                lock (_lock) {
                    List<Bomb> explodedBombs = [];
                    foreach (var bomb in _map.Bombs) {
                        if ((DateTime.Now - bomb.PlaceTime).TotalSeconds >= 2.0) {
                            _map.ExplodeBomb(bomb.Position.X, bomb.Position.Y);
                            BroadcastToAll(new NetworkMessage(MessageType.ExplodeBomb, new Dictionary<string, string> {
                                { "x", bomb.Position.X.ToString() },
                                { "y", bomb.Position.Y.ToString() },
                                { "positions", string.Join(";", bomb.ExplosionPositions) }
                            }));
                            explodedBombs.Add(bomb);
                        }
                    }

                    foreach (var bomb in explodedBombs) {
                        foreach (var pos in bomb.ExplosionPositions) {
                            CheckForPlayersInExplosion(pos.X, pos.Y);
                        }

                        _map.Bombs.Remove(bomb);
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
                        { "y", respawnPosition.Y.ToString() }
                    }));
                }
            }
        }

        private Position GenerateInitialPosition() {
            Random rand = new();
            int x, y;
            do {
                x = rand.Next(0, _map.Height);
                y = rand.Next(0, _map.Width);
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
                case MessageType.MovePlayer:
                    Direction direction = Enum.Parse<Direction>(message.Data["direction"]);
                    _map.MovePlayer(playerId, direction);
                    BroadcastToAll(new NetworkMessage(MessageType.MovePlayer, new Dictionary<string, string> {
                        { "playerId", playerId.ToString() },
                        { "x", _map.GetPlayerPosition(playerId).X.ToString() },
                        { "y", _map.GetPlayerPosition(playerId).Y.ToString() }
                    }));
                    break;
                case MessageType.PlaceBomb:
                    int x = int.Parse(message.Data["x"]);
                    int y = int.Parse(message.Data["y"]);
                    BombType type = Enum.Parse<BombType>(message.Data["type"]);
                    _map.AddBomb(x, y, type);
                    BroadcastToAll(new NetworkMessage(MessageType.PlaceBomb, new Dictionary<string, string> {
                        { "x", x.ToString() },
                        { "y", y.ToString() },
                        { "type", type.ToString() }
                    }));
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
