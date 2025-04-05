using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class Server
    {
        private TcpListener _server;
        private List<ClientHandler> _clients = new List<ClientHandler>();
        private Dictionary<int, Point> _playerPositions = new Dictionary<int, Point>();
        private List<Bomb> _bombs = new List<Bomb>();
        private int _nextPlayerId = 0;
        private const int GridWidth = 15;
        private const int GridHeight = 13;
        private readonly object _lock = new object();
        private const int ExplosionRange = 1; // How far the explosion reaches in each direction
        private static readonly int[] DirectionsX = { -1, 0, 1, 0 };
        private static readonly int[] DirectionsY = { 0, -1, 0, 1 };

        public class ClientHandler
        {
            public int PlayerId { get; set; }
            public TcpClient Client { get; set; }
            public NetworkStream Stream { get; set; }
        }

        public class Bomb
        {
            public Point Position { get; set; }
            public DateTime PlacedTime { get; set; }
            public int PlacedBy { get; set; }
        }

        public void Start()
        {
            _server = new TcpListener(IPAddress.Any, 5000);
            _server.Start();
            Console.WriteLine("Server is running on port 5000...");

            // Start bomb timer thread
            Thread bombThread = new Thread(ProcessBombs);
            bombThread.IsBackground = true;
            bombThread.Start();

            // Start player position broadcast thread
            Thread broadcastThread = new Thread(BroadcastPlayerPositions);
            broadcastThread.IsBackground = true;
            broadcastThread.Start();

            while (true)
            {
                try
                {
                    // Accept new client connection
                    TcpClient client = _server.AcceptTcpClient();
                    int playerId = _nextPlayerId++;

                    ClientHandler clientHandler = new ClientHandler
                    {
                        PlayerId = playerId,
                        Client = client,
                        Stream = client.GetStream()
                    };

                    lock (_lock)
                    {
                        _clients.Add(clientHandler);
                        // Set initial player position
                        Point initialPos = new Point(1, 1);
                        _playerPositions[playerId] = initialPos;
                    }

                    Console.WriteLine($"Client connected: Player {playerId}");

                    // Send player ID to the client
                    SendToClient(clientHandler, $"playerId:{playerId}");

                    // Start thread to handle this client
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.IsBackground = true;
                    clientThread.Start(clientHandler);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private void ProcessBombs()
        {
            while (true)
            {
                List<Bomb> explodedBombs = new List<Bomb>();

                lock (_lock)
                {
                    // Check for bombs that need to explode
                    foreach (var bomb in _bombs)
                    {
                        if ((DateTime.Now - bomb.PlacedTime).TotalSeconds >= 2.0) // 2-second timer
                        {
                            explodedBombs.Add(bomb);
                            Console.WriteLine($"Bomb at {bomb.Position.X},{bomb.Position.Y} is exploding!");
                        }
                    }

                    // Process exploded bombs
                    foreach (var bomb in explodedBombs)
                    {
                        _bombs.Remove(bomb);
                        // Center
                        BroadcastToAll($"explosion:{bomb.Position.X},{bomb.Position.Y}");
                        Console.WriteLine($"Explosion at {bomb.Position.X},{bomb.Position.Y}");

                        // Check in all 4 directions (up, down, left, right)
                        for (int i = 0; i < 4; i++)
                        {
                            int dx = DirectionsX[i];
                            int dy = DirectionsY[i];

                            for (int j = 1; j <= ExplosionRange; j++)
                            {
                                int xPos = bomb.Position.X + dx * j;
                                int yPos = bomb.Position.Y + dy * j;
                                if (IsValidPosition(xPos, yPos))
                                {
                                    BroadcastToAll($"explosion:{xPos},{yPos}");
                                    Console.WriteLine($"Explosion at {xPos},{yPos}");
                                }
                                else
                                    break; // Stop if we hit a wall or edge
                            }
                        }

                        // Check for players hit by explosion
                        CheckForPlayersInExplosion(bomb);
                    }
                }

                Thread.Sleep(100); // Check every 100ms
            }
        }

        private void CheckForPlayersInExplosion(Bomb bomb)
        {
            // List of positions affected by the explosion
            List<Point> explosionArea = new List<Point>();

            // Add bomb center
            explosionArea.Add(bomb.Position);

            // Add positions in each direction
            for (int i = 0; i < 4; i++)
            {
                int dx = DirectionsX[i];
                int dy = DirectionsY[i];

                for (int j = 1; j <= ExplosionRange; j++)
                {
                    int xPos = bomb.Position.X + dx * j;
                    int yPos = bomb.Position.Y + dy * j;
                    if (IsValidPosition(xPos, yPos))
                    {
                        explosionArea.Add(new Point(xPos, yPos));
                    }
                    else
                        break; // Stop if we hit a wall or edge
                }
            }

            // Check each player
            foreach (var player in new Dictionary<int, Point>(_playerPositions))
            {
                if (explosionArea.Exists(p => p.X == player.Value.X && p.Y == player.Value.Y))
                {
                    // Player was hit - respawn them
                    RespawnPlayer(player.Key);
                }
            }
        }

        private void RespawnPlayer(int playerId)
        {
            // Respawn player at appropriate position
            Point respawnPoint;
            respawnPoint = new Point(1, 1); // Top-left
            _playerPositions[playerId] = respawnPoint;

            // // Inform the player they've been hit
            // SendToPlayerById(playerId, $"hit:1");

            // Inform all players of the hit
            BroadcastToAll($"hit:{playerId}");
            Console.WriteLine($"Player {playerId} was hit and respawned at {respawnPoint.X},{respawnPoint.Y}");
        }

        private void BroadcastPlayerPositions()
        {
            while (true)
            {
                if (_clients.Count > 0)
                {
                    StringBuilder positionsMsg = new StringBuilder("playerPositions:");

                    lock (_lock)
                    {
                        foreach (var player in _playerPositions)
                        {
                            positionsMsg.Append($"{player.Key},{player.Value.X},{player.Value.Y};");
                        }
                    }

                    BroadcastToAll(positionsMsg.ToString());
                }

                Thread.Sleep(50); // Update 20 times per second
            }
        }

        private void HandleClient(object obj)
        {
            ClientHandler handler = (ClientHandler)obj;
            NetworkStream stream = handler.Stream;
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessClientMessage(handler.PlayerId, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client {handler.PlayerId}: {ex.Message}");
            }
            finally
            {
                // Client disconnected
                lock (_lock)
                {
                    _clients.Remove(handler);
                    _playerPositions.Remove(handler.PlayerId);
                }
                Console.WriteLine($"Player {handler.PlayerId} disconnected");

                try
                {
                    handler.Stream.Close();
                    handler.Client.Close();
                }
                catch { }
            }
        }

        private void ProcessClientMessage(int playerId, string message)
        {
            try
            {
                string[] parts = message.Split(':');
                if (parts.Length < 2) return;

                string command = parts[0];
                string data = parts[1];

                switch (command)
                {
                    case "move":
                        string[] moveCoords = data.Split(',');
                        int moveX = int.Parse(moveCoords[0]);
                        int moveY = int.Parse(moveCoords[1]);

                        lock (_lock)
                        {
                            if (IsValidMove(moveX, moveY))
                            {
                                _playerPositions[playerId] = new Point(moveX, moveY);
                            }
                        }
                        break;

                    case "bomb":
                        string[] bombCoords = data.Split(',');
                        int bombX = int.Parse(bombCoords[0]);
                        int bombY = int.Parse(bombCoords[1]);

                        lock (_lock)
                        {
                            if (IsValidPosition(bombX, bombY))
                            {
                                Point bombPosition = new Point(bombX, bombY);
                                bool bombExists = false;

                                // Check if bomb already exists at this position
                                foreach (var bomb in _bombs)
                                {
                                    if (bomb.Position.X == bombPosition.X && bomb.Position.Y == bombPosition.Y)
                                    {
                                        bombExists = true;
                                        break;
                                    }
                                }

                                if (!bombExists)
                                {
                                    _bombs.Add(new Bomb
                                    {
                                        Position = bombPosition,
                                        PlacedTime = DateTime.Now,
                                        PlacedBy = playerId
                                    });

                                    BroadcastToAll($"bomb:{bombX},{bombY}");
                                    Console.WriteLine($"Player {playerId} placed bomb at {bombX},{bombY}");
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        private bool IsValidMove(int x, int y)
        {
            return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
        }

        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
        }

        private void BroadcastToAll(string message)
        {
            message += "|"; // Append a delimiter to the message
            byte[] data = Encoding.UTF8.GetBytes(message);

            lock (_lock)
            {
                foreach (var client in _clients)
                {
                    try
                    {
                        client.Stream.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        // Ignore errors during broadcast, disconnected clients will be removed by their handler thread
                    }
                }
            }
        }

        private void SendToClient(ClientHandler client, string message)
        {
            message += "|";
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                client.Stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending to client {client.PlayerId}: {ex.Message}");
            }
        }

        private void SendToPlayerById(int playerId, string message)
        {
            lock (_lock)
            {
                var client = _clients.Find(c => c.PlayerId == playerId);
                if (client != null)
                {
                    SendToClient(client, message);
                }
            }
        }

        // Helper structure to represent positions
        public struct Point
        {
            public int X, Y;
            public Point(int x, int y) { X = x; Y = y; }
        }
    }
}