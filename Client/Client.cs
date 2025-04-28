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
        private SceneNode _sceneGraph;
        private SceneNode _mapLayer, _bombLayer, _playerLayer;

        private const int TILE_SIZE = 48;

        private int _playerId = -1;
        private Map _map = null;
        private readonly Dictionary<int, PlayerNode> _playerNodes = [];
        private readonly Dictionary<(int, int), SpriteNode> _bombSprites = [];

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _connected = false;

        public ClientGame() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize() {
            _sceneGraph = new SceneNode();
            _mapLayer = new SceneNode();
            _bombLayer = new SceneNode();
            _playerLayer = new SceneNode();
            _sceneGraph.AttachChild(_mapLayer);
            _sceneGraph.AttachChild(_bombLayer);
            _sceneGraph.AttachChild(_playerLayer);

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
                Console.WriteLine("Connected to server");
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
            switch (message.Type) {
                case MessageType.InitMap: {
                        _playerId = int.Parse(message.Data["playerId"]);
                        _map = Map.FromString(message.Data["map"]);

                        _graphics.PreferredBackBufferHeight = _map.Height * TILE_SIZE;
                        _graphics.PreferredBackBufferWidth = _map.Width * TILE_SIZE;
                        _graphics.ApplyChanges();

                        (int, int)[] directions = [
                            new(-1, 0),
                            new(1, 0),
                            new(0, -1),
                            new(0, 1),
                        ];
                        for (int i = 0; i < _map.Height; i++) {
                            for (int j = 0; j < _map.Width; j++) {
                                SpriteNode cellSprite = new(TextureHolder.Get("Texture/Tileset/TilesetField", new Rectangle(16, 16, 16, 16)), new Vector2(TILE_SIZE, TILE_SIZE)) {
                                    Position = new Vector2(j * TILE_SIZE, i * TILE_SIZE)
                                };
                                _mapLayer.AttachChild(cellSprite);

                                if (_map.GetTile(i, j) != TileType.Wall) {
                                    continue;
                                }

                                bool[,] localArea = new bool[3, 3];
                                for (int x = -1; x <= 1; x++) {
                                    for (int y = -1; y <= 1; y++) {
                                        int newX = i + x;
                                        int newY = j + y;
                                        if (_map.IsInBounds(newX, newY)) {
                                            localArea[x + 1, y + 1] = _map.GetTile(newX, newY) == TileType.Wall;
                                        } else {
                                            localArea[x + 1, y + 1] = false;
                                        }
                                    }
                                }

                                (int, int) p = BitmaskReferences.GetPosition(localArea);
                                SpriteNode wallSprite = new(TextureHolder.Get("Texture/Tileset/TilesetFloor", new Rectangle(p.Item2 * 16, p.Item1 * 16, 16, 16)), new Vector2(TILE_SIZE, TILE_SIZE)) {
                                    Position = new Vector2(j * TILE_SIZE, i * TILE_SIZE)
                                };
                                _mapLayer.AttachChild(wallSprite);
                            }
                        }
                    }
                    break;
                case MessageType.InitPlayer: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int skinId = int.Parse(message.Data["skinId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        _map.SetPlayerPosition(playerId, x, y);

                        PlayerNode playerNode = new(TextureHolder.Get($"Texture/Character/{(PlayerSkin)skinId}"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                            Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE)
                        };
                        _playerLayer.AttachChild(playerNode);
                        _playerNodes.Add(playerId, playerNode);
                    }
                    break;
                case MessageType.MovePlayer: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        Direction direction = Enum.Parse<Direction>(message.Data["d"]);
                        _map.SetPlayerPosition(playerId, x, y);

                        _playerNodes[playerId].MoveTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), direction, 0.2f);
                    }
                    break;
                case MessageType.RemovePlayer: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        _map.PlayerPositions.Remove(playerId);
                    }
                    break;
                case MessageType.PlaceBomb: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        BombType type = Enum.Parse<BombType>(message.Data["type"]);
                        _map.AddBomb(x, y, type);
                        _bombSprites.Add((x, y), new(TextureHolder.Get("Texture/Item/Dynamite"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                            Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE)
                        });
                        _bombLayer.AttachChild(_bombSprites[(x, y)]);
                    }
                    break;
                case MessageType.ExplodeBomb: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        string[] positions = message.Data["positions"].Split(';');
                        int bombId = _map.Bombs.FindIndex(b => b.Position.X == x && b.Position.Y == y);
                        foreach (var pos in positions) {
                            _bombLayer.AttachChild(new ExplosionNode(TextureHolder.Get("Texture/Effect/Explosion"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                                Position = new Vector2(Position.FromString(pos).Y * TILE_SIZE, Position.FromString(pos).X * TILE_SIZE)
                            });
                        }
                        _map.RemoveBomb(x, y);
                        _bombLayer.DetachChild(_bombSprites[(x, y)]);
                        _bombSprites.Remove((x, y));
                    }
                    break;
                case MessageType.RespawnPlayer: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        Direction direction = Enum.Parse<Direction>(message.Data["d"]);
                        _map.SetPlayerPosition(playerId, x, y);
                        _playerNodes[playerId].MoveTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), direction, 0.2f);
                    }
                    break;
                default:
                    Console.WriteLine($"Unknown message type: {message.Type}");
                    break;
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
            TextureHolder.SetContentManager(Content);
        }

        protected override void Update(GameTime gameTime) {
            HandleUpdate(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.White);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try {
                HandleDraw();
            } catch (Exception ex) {
                Console.WriteLine($"Error during draw: {ex.Message}");
            }
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void HandleUpdate(GameTime gameTime) {
            _sceneGraph.UpdateTree(gameTime);

            if (_map == null) {
                return;
            }

            KeyboardState key = Keyboard.GetState();

            if (true) {
                Direction direction = Direction.None;
                if (_playerNodes.ContainsKey(_playerId) && !_playerNodes[_playerId].Moving) {
                    if (key.IsKeyDown(Keys.Up)) {
                        direction = Direction.Up;
                    } else if (key.IsKeyDown(Keys.Down)) {
                        direction = Direction.Down;
                    } else if (key.IsKeyDown(Keys.Left)) {
                        direction = Direction.Left;
                    } else if (key.IsKeyDown(Keys.Right)) {
                        direction = Direction.Right;
                    }
                }

                if (direction != Direction.None) {
                    SendMessage(new NetworkMessage(MessageType.MovePlayer, new Dictionary<string, string> {
                        { "direction", direction.ToString() }
                    }));
                }
            }

            if (true) {
                if (key.IsKeyDown(Keys.Space)) {
                    SendMessage(new NetworkMessage(MessageType.PlaceBomb, new Dictionary<string, string> {
                        { "x", _map.GetPlayerPosition(_playerId).X.ToString() },
                        { "y", _map.GetPlayerPosition(_playerId).Y.ToString() },
                        { "type", BombType.Normal.ToString() },
                    }));
                } else if (key.IsKeyDown(Keys.Enter)) {
                    SendMessage(new NetworkMessage(MessageType.PlaceBomb, new Dictionary<string, string> {
                        { "x", _map.GetPlayerPosition(_playerId).X.ToString() },
                        { "y", _map.GetPlayerPosition(_playerId).Y.ToString() },
                        { "type", BombType.Special.ToString() },
                    }));
                }
            }
        }

        private void HandleDraw() {
            _sceneGraph.DrawTree(_spriteBatch, Matrix.Identity);
        }

        protected override void UnloadContent() {
            if (_connected) {
                _connected = false;
                _stream.Close();
                _client.Close();
            }

            TextureHolder.UnloadAll();
            base.UnloadContent();
        }
    }
}
