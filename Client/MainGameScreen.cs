using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.Json;


using Shared;
using Client.Component;
using Client.PowerUps;

namespace Client {
    public class MainGameScreen : GameScreen {
        private readonly object _lock = new();

        private SceneNode _sceneGraph;
        private SceneNode _mapLayer, _bombLayer, _playerLayer, _itemLayer, _vfxLayer;

        private readonly TextNode _pingText = new("Ping: ?ms");

        private const int TILE_SIZE = 48;

        private Map _map = null;
        private readonly Dictionary<int, PlayerNode> _playerNodes = [];
        private readonly Dictionary<(int, int), BombNode> _bombNodes = [];
        private readonly Dictionary<(int, int), SpriteNode> _grassNodes = [];
        private readonly Dictionary<(int, int), ItemNode> _itemNodes = [];
        private Dictionary<int, int> _skinMapping = [];

        private LinearLayout _sidebar;
        private Scoreboard _scoreboard;
        private PowerSlot _powerSlot;

        public MainGameScreen() { }

        public override void Initialize() {
            _sidebar = new LinearLayout(LinearLayout.Orientation.Vertical, hasBackground: false, padding: 0) {
                Position = new Vector2(0, 0),
                Size = new Vector2(240, 720),
                Spacing = 0,
            };

            _powerSlot = new PowerSlot();
            _sidebar.AddComponent(_powerSlot, weight: 2);
            _sidebar.AddComponent(new Button() {
                Text = "Leave Game",
                OnClick = () => {
                    NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.LeaveRoom));
                    ScreenManager.Instance.NavigateToRoot();
                },
            });
            uiManager.AddComponent(_sidebar);

            _sceneGraph = new SceneNode();
            _mapLayer = new SceneNode();
            _bombLayer = new SceneNode();
            _itemLayer = new SceneNode();
            _playerLayer = new SceneNode();
            _vfxLayer = new SceneNode();
            _sceneGraph.AttachChild(_mapLayer);
            _sceneGraph.AttachChild(_bombLayer);
            _sceneGraph.AttachChild(_itemLayer);
            _sceneGraph.AttachChild(_playerLayer);
            _sceneGraph.AttachChild(_vfxLayer);
            _sceneGraph.AttachChild(_pingText);

            _sceneGraph.Position = new Vector2(240, 0);
            _pingText.Position = new Vector2(10 * TILE_SIZE, 14 * TILE_SIZE + 10);
        }

        public override void Activate() {
            base.Activate();
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetGameInfo));
        }

        public override void HandleResponse(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.GameInfo: {
                        _map = Map.FromString(message.Data["map"]);
                        int playerCount = int.Parse(message.Data["playerCount"]);
                        int[] playerIds = Array.ConvertAll(message.Data["playerIds"].Split(';'), int.Parse);
                        string[] usernames = message.Data["usernames"].Split(';');
                        Position[] playerPositions = Array.ConvertAll(message.Data["playerPositions"].Split(';'), Position.FromString);
                        ProcessMap();
                        List<(string, string, int)> playerData = new();
                        for (int i = 0; i < playerCount; i++) {
                            int playerId = playerIds[i];
                            string username = usernames[i];
                            int x = playerPositions[i].X;
                            int y = playerPositions[i].Y;
                            _map.SetPlayerPosition(playerId, x, y);

                            PlayerNode playerNode = new(TextureHolder.Get($"Texture/Character/{(PlayerSkin)i}"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                                Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE)
                            };

                            _playerLayer.AttachChild(playerNode);
                            _playerNodes.Add(playerId, playerNode);
                            _skinMapping.Add(playerId, i);
                            playerData.Add((playerId.ToString(), username, i));
                        }

                        _scoreboard = new Scoreboard(playerData) {
                            Position = new Vector2(0, 0),
                            Size = new Vector2(240, Client.Instance.GraphicsDevice.Viewport.Height)
                        };

                        _sidebar.AddComponent(_scoreboard, 6, 0);
                    }
                    break;
                case ServerMessageType.PlayerMoved: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        Direction direction = Enum.Parse<Direction>(message.Data["d"]);

                        if (x != _map.PlayerInfos[playerId].Position.X || y != _map.PlayerInfos[playerId].Position.Y) {
                            lock (_lock) {
                                _map.SetPlayerPosition(playerId, x, y);
                                _playerNodes[playerId].MoveTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), direction);
                            }
                        }
                    }
                    break;
                case ServerMessageType.PlayerLeft: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        lock (_lock) {
                            _map.PlayerInfos.Remove(playerId);
                            if (_playerNodes.ContainsKey(playerId)) {
                                _playerLayer.DetachChild(_playerNodes[playerId]);
                                _playerNodes.Remove(playerId);
                            }
                        }
                    }
                    break;
                case ServerMessageType.BombPlaced: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        BombType type = Enum.Parse<BombType>(message.Data["type"]);

                        lock (_lock) {
                            if (_map.HasBomb(x, y)) {
                                _map.RemoveBomb(x, y);
                            }
                            _map.AddBomb(x, y, type);
                            _bombNodes.Add((x, y), new(TextureHolder.Get("Texture/Item/Dynamite"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                                Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE)
                            });
                            _bombLayer.AttachChild(_bombNodes[(x, y)]);
                        }
                    }
                    break;
                case ServerMessageType.BombExploded: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        string[] positions = message.Data["positions"].Split(';');

                        lock (_lock) {
                            foreach (var pos in positions) {
                                int ex = Position.FromString(pos).X;
                                int ey = Position.FromString(pos).Y;

                                _bombLayer.AttachChild(new ExplosionNode(TextureHolder.Get("Texture/Effect/Explosion"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                                    Position = new Vector2(ey * TILE_SIZE, ex * TILE_SIZE)
                                });

                                if (_map.GetTile(ex, ey) == TileType.Grass) {
                                    _bombLayer.DetachChild(_grassNodes[(ex, ey)]);
                                    _grassNodes.Remove((ex, ey));
                                    _map.SetTile(ex, ey, TileType.Empty);
                                }
                            }
                            _map.RemoveBomb(x, y);
                            _bombLayer.DetachChild(_bombNodes[(x, y)]);
                            _bombNodes.Remove((x, y));
                        }
                    }
                    break;
                case ServerMessageType.PlayerDied: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int byPlayerId = int.Parse(message.Data["byPlayerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);

                        lock (_lock) {
                            _map.SetPlayerPosition(playerId, x, y);
                            _playerNodes[playerId].Die();
                            _playerNodes[playerId].TeleportTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), Direction.Down);
                            if (playerId != byPlayerId) {
                                IncreaseScore(byPlayerId);
                            }
                        }
                    }
                    break;
                case ServerMessageType.GameStopped: {
                        Console.WriteLine("Game stopped");
                        ScreenManager.Instance.NavigateTo(ScreenName.EndGameScreen, isOverlay: true, new(){
                            { "skinMapping", _skinMapping },
                        });
                    }
                    break;

                case ServerMessageType.PowerUpUsed: {
                        string powerUpType = message.Data["powerUpType"];
                        Dictionary<string, object> parameters = message.Data["parameters"] != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(message.Data["parameters"]) : new Dictionary<string, object>();
                        parameters["vfxLayer"] = _vfxLayer;
                        parameters["playerNodes"] = _playerNodes;
                        parameters["map"] = _map;
                        PowerUp powerUp = PowerUpFactory.CreatePowerUp(Enum.Parse<PowerName>(powerUpType));
                        lock (_lock) {
                            powerUp.Apply(parameters);
                        }
                    }
                    break;

                case ServerMessageType.PowerUpSpawned: {
                        PowerName powerUpType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        lock (_lock) {
                            ItemNode itemNode = new ItemNode(powerUpType) {
                                Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE),
                            };
                            _map.AddItem(x, y, powerUpType);
                            _itemNodes.Add((x, y), itemNode);
                            _itemLayer.AttachChild(itemNode);
                        }
                    }
                    break;

                case ServerMessageType.PowerUpPickedUp: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        PowerName powerUpType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        lock (_lock) {
                            _map.RemoveItem(x, y);
                            _map.PlayerInfos[playerId].PickUpItem(powerUpType);
                            _itemLayer.DetachChild(_itemNodes[(x, y)]);
                            _itemNodes.Remove((x, y));
                            if (playerId == NetworkManager.Instance.ClientId) {
                                _powerSlot.ObtainPower(powerUpType);
                            }
                        }
                    }
                    break;
                case ServerMessageType.ItemExpired: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        lock (_lock) {
                            _map.RemoveItem(x, y);
                            _itemLayer.DetachChild(_itemNodes[(x, y)]);
                            _itemNodes.Remove((x, y));
                        }
                    }
                    break;
            }
        }

        private void ProcessMap() {
            for (int i = 0; i < _map.Height; i++) {
                for (int j = 0; j < _map.Width; j++) {
                    SpriteNode cellSprite = new(TextureHolder.Get("Texture/Tileset/TilesetField", new Rectangle(16, 16, 16, 16)), new Vector2(TILE_SIZE, TILE_SIZE)) {
                        Position = new Vector2(j * TILE_SIZE, i * TILE_SIZE)
                    };
                    _mapLayer.AttachChild(cellSprite);

                    if (_map.GetTile(i, j) == TileType.Grass) {
                        SpriteNode grassSprite = new(TextureHolder.Get("Texture/Tileset/TilesetNature", new Rectangle(96, 240, 16, 16)), new Vector2(TILE_SIZE, TILE_SIZE)) {
                            Position = new Vector2(j * TILE_SIZE, i * TILE_SIZE)
                        };

                        _grassNodes.Add((i, j), grassSprite);
                        _bombLayer.AttachChild(grassSprite);
                    }

                    if (_map.GetTile(i, j) != TileType.Wall) {
                        continue;
                    }

                    bool[,] localArea = new bool[3, 3];
                    for (int u = -1; u <= 1; u++) {
                        for (int v = -1; v <= 1; v++) {
                            int newX = i + u;
                            int newY = j + v;
                            if (_map.IsInBounds(newX, newY)) {
                                localArea[u + 1, v + 1] = _map.GetTile(newX, newY) == TileType.Wall;
                            } else {
                                localArea[u + 1, v + 1] = false;
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

        private void IncreaseScore(int playerId) {
            _scoreboard.IncreaseScore(playerId);
        }

        private void HandleUpdate(GameTime gameTime) {
            _sceneGraph.UpdateTree(gameTime);

            _pingText.Text = $"Ping: {NetworkManager.Instance.Ping}ms";
            if (NetworkManager.Instance.Ping > 200) {
                _pingText.Color = Color.Red;
            } else {
                _pingText.Color = Color.White;
            }

            int playerId = NetworkManager.Instance.ClientId;
            if (_map == null || !_map.PlayerInfos.ContainsKey(playerId) || !_playerNodes.ContainsKey(playerId)) {
                return;
            }

            KeyboardState key = Keyboard.GetState();

            if (true) {
                Direction direction = Direction.None;
                if (!_playerNodes[playerId].Moving) {
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

                if (direction != Direction.None && !_map.IsPlayerMovable(playerId, direction)) {
                    _playerNodes[playerId].SetDirection(direction);
                    direction = Direction.None;
                }

                if (direction != Direction.None) {
                    _map.MovePlayer(playerId, direction);
                    int x = _map.PlayerInfos[playerId].Position.X;
                    int y = _map.PlayerInfos[playerId].Position.Y;
                    _playerNodes[playerId].MoveTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), direction);
                    NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.MovePlayer, new() {
                        { "direction", direction.ToString() }
                    }));
                }
            }

            if (key.IsKeyDown(Keys.Space) || key.IsKeyDown(Keys.Enter)) {
                var currentPos = _playerNodes[playerId].Position;
                var nearestCell = GetNearestEmptyCell(currentPos.X, currentPos.Y);

                Position GetNearestEmptyCell(float startX, float startY) {
                    Position nearest = null;
                    float minDistance = float.MaxValue;

                    lock (_lock) {
                        for (int i = 0; i < _map.Height; i++) {
                            for (int j = 0; j < _map.Width; j++) {
                                if (_map.GetTile(i, j) == TileType.Empty && !_map.HasBomb(i, j)) {
                                    float distance = Math.Abs(startX - (j * TILE_SIZE)) + Math.Abs(startY - (i * TILE_SIZE));
                                    if (distance < TILE_SIZE / 3 && distance < minDistance) {
                                        minDistance = distance;
                                        nearest = new Position(i, j);
                                    }
                                }
                            }
                        }
                    }

                    return nearest;
                }

                if (nearestCell != null) {
                    lock (_lock) {
                        _map.AddBomb(nearestCell.X, nearestCell.Y, BombType.Normal);
                    }

                    if (key.IsKeyDown(Keys.Space)) {
                        NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                            { "x", nearestCell.X.ToString() },
                            { "y", nearestCell.Y.ToString() },
                            { "type", BombType.Normal.ToString() },
                        }));
                    } else if (key.IsKeyDown(Keys.Enter)) {
                        NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                            { "x", nearestCell.X.ToString() },
                            { "y", nearestCell.Y.ToString() },
                            { "type", BombType.Special.ToString() },
                        }));
                    }
                }
            }

            if (key.IsKeyDown(Keys.E) || key.IsKeyDown(Keys.Q)) {
                _powerSlot.UsePower(key.IsKeyDown(Keys.E) ? 'E' : 'Q');
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            HandleUpdate(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            base.Draw(gameTime, spriteBatch);
            try {
                _sceneGraph.DrawTree(spriteBatch, Matrix.Identity);
            } catch (Exception ex) {
                Console.WriteLine($"Error during draw: {ex.Message}");
            }
        }
    }
}
