using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Shared;

namespace Client {
    public class ClientGame : Game {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private const int TILE_SIZE = 48;

        private Texture2D _tileTexture;
        private Texture2D _gridLineTexture;
        private Texture2D _playerTexture;
        private Texture2D _otherPlayerTexture;
        private Texture2D _bombTexture;
        private Texture2D _explosionTexture;

        private int _playerId = -1;
        private Map _map = new Map(13, 15);

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _connected = false;

        private KeyboardState _prevKeyState;

        public ClientGame() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferHeight = _map.Height * TILE_SIZE;
            _graphics.PreferredBackBufferWidth = _map.Width * TILE_SIZE;
        }

        protected override void Initialize() {
            ConnectToServer();
            base.Initialize();
        }

        private void ConnectToServer() {
            try {
                _client = new TcpClient("localhost", 5000);
                _stream = _client.GetStream();
                _connected = true;

                _receiveThread = new Thread(ReceiveMessages);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();
                Console.WriteLine("Connected to server.");
            } catch (Exception ex) {
                Console.WriteLine($"Connection failed: {ex.Message}");
                _connected = false;
            }
        }

        private void ReceiveMessages() {
            byte[] buffer = new byte[1024];

            while (_connected) {
                try {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0) {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string[] messages = message.Split(['\n', '\r', '|'], StringSplitOptions.RemoveEmptyEntries);

                        foreach (var msg in messages) {
                            if (!string.IsNullOrEmpty(msg)) {
                                ProcessServerMessage(NetworkMessage.FromJson(msg));
                            }
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    _connected = false;
                    break;
                }
            }
        }

        private void ProcessServerMessage(NetworkMessage message) {
            if (message.Type == MessageType.PlayerId) {
                _playerId = int.Parse(message.Data["playerId"]);
            } else if (message.Type == MessageType.MovePlayer) {
                int playerId = int.Parse(message.Data["playerId"]);
                int x = int.Parse(message.Data["x"]);
                int y = int.Parse(message.Data["y"]);
                _map.SetPlayerPosition(playerId, x, y);
            } else if (message.Type == MessageType.RemovePlayer) {
                int playerId = int.Parse(message.Data["playerId"]);
                _map.PlayerPositions.Remove(playerId);
            } else if (message.Type == MessageType.PlaceBomb) {
                int x = int.Parse(message.Data["x"]);
                int y = int.Parse(message.Data["y"]);
                BombType type = Enum.Parse<BombType>(message.Data["type"]);
                _map.AddBomb(x, y, type);
            } else if (message.Type == MessageType.ExplodeBomb) {
                int x = int.Parse(message.Data["x"]);
                int y = int.Parse(message.Data["y"]);
                string[] positions = message.Data["positions"].Split(';');
                int bombId = _map.Bombs.FindIndex(b => b.Position.X == x && b.Position.Y == y);
                foreach (var pos in positions) {
                    _map.Bombs[bombId].ExplosionPositions.Add(Position.FromString(pos));
                }
                _map.Bombs[bombId].ExplodeTime = DateTime.Now;
            } else if (message.Type == MessageType.RespawnPlayer) {
                int playerId = int.Parse(message.Data["playerId"]);
                int x = int.Parse(message.Data["x"]);
                int y = int.Parse(message.Data["y"]);
                _map.SetPlayerPosition(playerId, x, y);
            }
        }

        private void SendMessage(NetworkMessage message) {
            if (!_connected) {
                return;
            }

            try {
                byte[] data = Encoding.UTF8.GetBytes(message.ToJson() + "|");
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            } catch (Exception ex) {
                Console.WriteLine($"Error sending message: {ex.Message}");
                _connected = false;
            }
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _tileTexture = new Texture2D(GraphicsDevice, 1, 1);
            _tileTexture.SetData([Color.White]);

            _gridLineTexture = new Texture2D(GraphicsDevice, 1, 1);
            _gridLineTexture.SetData([Color.Black]);

            _playerTexture = new Texture2D(GraphicsDevice, 1, 1);
            _playerTexture.SetData([Color.Blue]);

            _otherPlayerTexture = new Texture2D(GraphicsDevice, 1, 1);
            _otherPlayerTexture.SetData([Color.Green]);

            _bombTexture = new Texture2D(GraphicsDevice, 1, 1);
            _bombTexture.SetData([Color.Red]);

            _explosionTexture = new Texture2D(GraphicsDevice, 1, 1);
            _explosionTexture.SetData([Color.Orange]);
        }

        private GameTime gameTime;

        protected override void Update(GameTime gameTime) {
            this.gameTime = gameTime;
            HandleUpdate();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin();
            HandleDraw();
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void HandleUpdate() {
            KeyboardState key = Keyboard.GetState();

            if (true) {
                Direction direction = Direction.None;
                if (IsKeyPressed(key, Keys.Up)) {
                    direction = Direction.Up;
                } else if (IsKeyPressed(key, Keys.Down)) {
                    direction = Direction.Down;
                } else if (IsKeyPressed(key, Keys.Left)) {
                    direction = Direction.Left;
                } else if (IsKeyPressed(key, Keys.Right)) {
                    direction = Direction.Right;
                }

                if (direction != Direction.None) {
                    SendMessage(new NetworkMessage(MessageType.MovePlayer, new Dictionary<string, string> {
                        { "direction", direction.ToString() }
                    }));
                }
            }

            if (IsKeyPressed(key, Keys.Space)) {
                SendMessage(new NetworkMessage(MessageType.PlaceBomb, new Dictionary<string, string> {
                    { "x", _map.GetPlayerPosition(_playerId).X.ToString() },
                    { "y", _map.GetPlayerPosition(_playerId).Y.ToString() },
                    { "type", BombType.Normal.ToString() },
                }));
            }

            _prevKeyState = key;

            for (int i = _map.Bombs.Count - 1; i >= 0; i--) {
                if (_map.Bombs[i].ExplodeTime != DateTime.MinValue && (DateTime.Now - _map.Bombs[i].ExplodeTime).TotalSeconds >= 0.5) {
                    _map.Bombs.RemoveAt(i);
                }
            }
        }

        private void HandleDraw() {
            for (int x = 0; x < _map.Height; x++) {
                for (int y = 0; y < _map.Width; y++) {
                    Rectangle cellRect = new Rectangle(y * TILE_SIZE, x * TILE_SIZE, TILE_SIZE, TILE_SIZE);
                    _spriteBatch.Draw(_tileTexture, cellRect, Color.White);
                    _spriteBatch.Draw(_gridLineTexture, new Rectangle(cellRect.X, cellRect.Y, TILE_SIZE, 1), Color.Black);
                    _spriteBatch.Draw(_gridLineTexture, new Rectangle(cellRect.X, cellRect.Y, 1, TILE_SIZE), Color.Black);
                }
            }

            foreach (var bomb in _map.Bombs) {
                if (bomb.ExplosionPositions.Count == 0) {
                    DrawCell(bomb.Position.X, bomb.Position.Y, _bombTexture);
                }
            }

            foreach (var bomb in _map.Bombs) {
                if (bomb.ExplosionPositions.Count > 0) {
                    foreach (var explosion in bomb.ExplosionPositions) {
                        DrawCell(explosion.X, explosion.Y, _explosionTexture);
                    }
                }
            }

            foreach (var player in _map.PlayerPositions) {
                if (player.Key != _playerId) {
                    DrawCell(player.Value.X, player.Value.Y, _otherPlayerTexture);
                }
            }

            if (_map.PlayerPositions.ContainsKey(_playerId)) {
                DrawCell(_map.GetPlayerPosition(_playerId).X, _map.GetPlayerPosition(_playerId).Y, _playerTexture);
            }
        }

        private void DrawCell(int x, int y, Texture2D texture) {
            Rectangle rect = new Rectangle(y * TILE_SIZE, x * TILE_SIZE, TILE_SIZE, TILE_SIZE);
            _spriteBatch.Draw(texture, rect, Color.White);
        }

        private bool IsKeyPressed(KeyboardState current, Keys key) {
            return current.IsKeyDown(key) && !_prevKeyState.IsKeyDown(key);
        }

        protected override void UnloadContent() {
            if (_connected) {
                _connected = false;
                _stream.Close();
                _client.Close();
            }

            base.UnloadContent();
        }
    }
}
