using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public class ClientGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _tileTexture;
        private Texture2D _gridLineTexture;
        private Texture2D _playerTexture;
        private Texture2D _otherPlayerTexture;
        private Texture2D _bombTexture;
        private Texture2D _explosionTexture;

        private const int TileSize = 48;
        private const int GridWidth = 15;
        private const int GridHeight = 13;

        private Point _playerGridPos = new(1, 1);
        private KeyboardState _prevKeyState;
        private int _playerId = -1;
        private Dictionary<int, Point> _otherPlayers = new Dictionary<int, Point>();

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _connected = false;

        // For hit animation
        private Dictionary<int, bool> _isHit = new Dictionary<int, bool>();
        private Dictionary<int, double> _hitAnimationTime = new Dictionary<int, double>();
        private const double HitAnimationDuration = 1.0; // 1 second for hit animation

        private class Bomb
        {
            public Point Position;
            public double PlacedTime;
        }

        private class Explosion
        {
            public Point Position;
            public double TriggerTime;
        }

        private List<Bomb> _bombs = new();
        private List<Explosion> _explosions = new();

        public ClientGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = TileSize * GridWidth;
            _graphics.PreferredBackBufferHeight = TileSize * GridHeight;
        }

        protected override void Initialize()
        {
            ConnectToServer();
            base.Initialize();
        }

        private void ConnectToServer()
        {
            try
            {
                _client = new TcpClient("localhost", 5000);
                _stream = _client.GetStream();
                _connected = true;

                // Start a thread to receive messages from the server
                _receiveThread = new Thread(ReceiveMessages);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();
                Console.WriteLine("Connected to server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                _connected = false;
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];

            while (_connected)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string[] messages = message.Split(new[] { '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var msg in messages)
                        {
                            if (!string.IsNullOrEmpty(msg))
                            {
                                ProcessServerMessage(msg);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    _connected = false;
                    break;
                }
            }
        }

        private void ProcessServerMessage(string message)
        {
            try
            {
                string[] parts = message.Split(':');

                if (parts.Length < 2) return;

                string command = parts[0];
                string data = parts[1];

                switch (command)
                {
                    case "playerId":
                        _playerId = int.Parse(data);
                        Console.WriteLine($"Assigned player ID: {_playerId}");
                        break;

                    case "playerPositions":
                        string[] positions = data.Split(';');
                        _otherPlayers.Clear();

                        foreach (string pos in positions)
                        {
                            if (string.IsNullOrEmpty(pos)) continue;

                            string[] playerData = pos.Split(',');
                            if (playerData.Length < 3) continue;

                            int playerId = int.Parse(playerData[0]);
                            int x = int.Parse(playerData[1]);
                            int y = int.Parse(playerData[2]);

                            if (playerId == _playerId)
                            {
                                // Update our own position (for synchronization)
                                _playerGridPos = new Point(x, y);
                                continue;
                            }

                            _otherPlayers[playerId] = new Point(x, y);
                        }
                        break;

                    case "bomb":
                        string[] bombPosition = data.Split(',');
                        if (bombPosition.Length < 2) break;

                        int bombX = int.Parse(bombPosition[0]);
                        int bombY = int.Parse(bombPosition[1]);

                        Point bombPos = new Point(bombX, bombY);
                        bool bombExists = false;

                        // Check if this bomb already exists
                        foreach (var bomb in _bombs)
                        {
                            if (bomb.Position == bombPos)
                            {
                                bombExists = true;
                                break;
                            }
                        }

                        if (!bombExists)
                        {
                            double currentTime = gameTime.TotalGameTime.TotalSeconds;
                            _bombs.Add(new Bomb
                            {
                                Position = bombPos,
                                PlacedTime = currentTime
                            });
                            Console.WriteLine($"Added bomb at {bombX},{bombY}");
                        }
                        break;

                    case "explosion":
                        Console.WriteLine($"Data: {data}");

                        string[] explosionPosition = data.Split(',');
                        if (explosionPosition.Length < 2) break;

                        int explosionX = int.Parse(explosionPosition[0]);
                        int explosionY = int.Parse(explosionPosition[1]);
                        Point explosionPos = new Point(explosionX, explosionY);

                        double explosionTime = gameTime.TotalGameTime.TotalSeconds;

                        // Add explosion at the specified position if not already there
                        // if (!_explosions.Exists(e => e.Position == explosionPos))
                        {
                            _explosions.Add(new Explosion { Position = explosionPos, TriggerTime = explosionTime });
                            Console.WriteLine($"Added explosion at {explosionX},{explosionY}");
                        }

                        // Remove any bomb at this position
                        _bombs.RemoveAll(b => b.Position == explosionPos);
                        break;

                    case "hit":
                        // Player was hit by an explosion
                        int hitPlayerId = int.Parse(data);
                        _isHit[hitPlayerId] = true;
                        _hitAnimationTime[hitPlayerId] = gameTime.TotalGameTime.TotalSeconds;
                        Console.WriteLine($"Player {hitPlayerId} was hit!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message '{message}': {ex.Message}");
            }
        }

        private GameTime gameTime;

        private void SendMessage(string message)
        {
            if (!_connected) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                _connected = false;
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _tileTexture = new Texture2D(GraphicsDevice, 1, 1);
            _tileTexture.SetData(new[] { Color.White });

            _gridLineTexture = new Texture2D(GraphicsDevice, 1, 1);
            _gridLineTexture.SetData(new[] { Color.Black });

            _playerTexture = new Texture2D(GraphicsDevice, 1, 1);
            _playerTexture.SetData(new[] { Color.Blue });

            _otherPlayerTexture = new Texture2D(GraphicsDevice, 1, 1);
            _otherPlayerTexture.SetData(new[] { Color.Green });

            _bombTexture = new Texture2D(GraphicsDevice, 1, 1);
            _bombTexture.SetData(new[] { Color.Red });

            _explosionTexture = new Texture2D(GraphicsDevice, 1, 1);
            _explosionTexture.SetData(new[] { Color.Orange });
        }

        protected override void Update(GameTime gameTime)
        {
            this.gameTime = gameTime;

            KeyboardState key = Keyboard.GetState();
            double currentTime = gameTime.TotalGameTime.TotalSeconds;

            // Process hit animation timeout
            foreach (var playerId in _isHit.Keys)
            {
                if (_isHit[playerId])
                {
                    if (currentTime - _hitAnimationTime[playerId] >= HitAnimationDuration)
                    {
                        _isHit[playerId] = false;
                    }
                }
            }

            Point direction = Point.Zero;
            bool hasMoved = false;

            if (IsKeyPressed(key, Keys.Left)) direction.X = -1;
            else if (IsKeyPressed(key, Keys.Right)) direction.X = 1;
            else if (IsKeyPressed(key, Keys.Up)) direction.Y = -1;
            else if (IsKeyPressed(key, Keys.Down)) direction.Y = 1;

            if (direction != Point.Zero)
            {
                Point newPos = _playerGridPos + direction;

                if (newPos.X >= 0 && newPos.X < GridWidth &&
                    newPos.Y >= 0 && newPos.Y < GridHeight)
                {
                    _playerGridPos = newPos;
                    hasMoved = true;

                    // Send move to server
                    SendMessage($"move:{_playerGridPos.X},{_playerGridPos.Y}");
                }
            }

            if (IsKeyPressed(key, Keys.Space))
            {
                // Commented out local check to allow server to decide
                // if (!_bombs.Exists(b => b.Position == _playerGridPos))
                // {
                // Send bomb placement to server
                SendMessage($"bomb:{_playerGridPos.X},{_playerGridPos.Y}");
                // }
            }

            // Remove expired explosions after 0.5 seconds
            _explosions.RemoveAll(e => currentTime - e.TriggerTime >= 0.5);

            _prevKeyState = key;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin();

            // Draw tiles with grid
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    Rectangle cellRect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                    _spriteBatch.Draw(_tileTexture, cellRect, Color.White);
                    _spriteBatch.Draw(_gridLineTexture, new Rectangle(cellRect.X, cellRect.Y, TileSize, 1), Color.Black); // top
                    _spriteBatch.Draw(_gridLineTexture, new Rectangle(cellRect.X, cellRect.Y, 1, TileSize), Color.Black); // left
                }
            }

            // Draw bombs
            foreach (var bomb in _bombs)
            {
                DrawCell(bomb.Position, _bombTexture);
            }

            // Draw explosions
            foreach (var exp in _explosions)
            {
                if (IsInsideGrid(exp.Position))
                    DrawCell(exp.Position, _explosionTexture);
            }

            // Draw other players
            foreach (var player in _otherPlayers)
            {
                if (_isHit.ContainsKey(player.Key) && _isHit[player.Key] && Math.Sin(gameTime.TotalGameTime.TotalSeconds * 10) > 0)
                {
                    Rectangle rect = new Rectangle(player.Value.X * TileSize, player.Value.Y * TileSize, TileSize, TileSize);
                    _spriteBatch.Draw(_otherPlayerTexture, rect, Color.Red); // Flashing red if hit
                }
                else
                {
                    DrawCell(player.Value, _otherPlayerTexture);
                }

            }

            // Draw player (flashing red if hit)
            if (_isHit.ContainsKey(_playerId) && _isHit[_playerId] && Math.Sin(gameTime.TotalGameTime.TotalSeconds * 10) > 0)
            {
                Rectangle rect = new Rectangle(_playerGridPos.X * TileSize, _playerGridPos.Y * TileSize, TileSize, TileSize);
                _spriteBatch.Draw(_playerTexture, rect, Color.Red); // Flashing red if hit
            }
            else
            {
                DrawCell(_playerGridPos, _playerTexture);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawCell(Point pos, Texture2D texture)
        {
            Rectangle rect = new Rectangle(pos.X * TileSize, pos.Y * TileSize, TileSize, TileSize);
            _spriteBatch.Draw(texture, rect, Color.White);
        }

        private bool IsKeyPressed(KeyboardState current, Keys key)
        {
            return current.IsKeyDown(key) && !_prevKeyState.IsKeyDown(key);
        }

        private bool IsInsideGrid(Point p)
        {
            return p.X >= 0 && p.X < GridWidth && p.Y >= 0 && p.Y < GridHeight;
        }

        protected override void UnloadContent()
        {
            if (_connected)
            {
                _connected = false;
                _stream.Close();
                _client.Close();
            }

            base.UnloadContent();
        }
    }
}