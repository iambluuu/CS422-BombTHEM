using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using Shared;
using Client.Component;

namespace Client {
    public class MainGameScreen : GameScreen {
        private ReaderWriterLockSlim _lock = new();

        private SceneNode _sceneGraph;
        private SceneNode _mapLayer, _bombLayer, _playerLayer;

        private const int TILE_SIZE = 48;

        private Map _map = null;
        private readonly Dictionary<int, PlayerNode> _playerNodes = [];
        private readonly Dictionary<(int, int), BombNode> _bombNodes = [];

        public MainGameScreen() { }

        public override void Initialize() {
            LinearLayout mainLayout = new LinearLayout(LinearLayout.Orientation.Vertical, spacing: 20) {
                Position = Vector2.Zero,
                Size = new Vector2(240, 720),
                Padding = 20,
            };

            Button leaveButton = new Button() {
                Position = Vector2.Zero,
                Size = Vector2.Zero,
                OnClick = () => {
                    NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.LeaveRoom));
                    ScreenManager.Instance.NavigateToRoot();
                },
                Text = "Leave",
            };

            mainLayout.AddComponent(leaveButton);
            uiManager.AddComponent(mainLayout);

            _sceneGraph = new SceneNode();
            _mapLayer = new SceneNode();
            _bombLayer = new SceneNode();
            _playerLayer = new SceneNode();
            _sceneGraph.AttachChild(_mapLayer);
            _sceneGraph.AttachChild(_bombLayer);
            _sceneGraph.AttachChild(_playerLayer);

            _sceneGraph.Position = new Vector2(240, 0);
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
                        Position[] playerPositions = Array.ConvertAll(message.Data["playerPositions"].Split(';'), Position.FromString);

                        ProcessMap();

                        for (int i = 0; i < playerCount; i++) {
                            int playerId = playerIds[i];
                            int x = playerPositions[i].X;
                            int y = playerPositions[i].Y;
                            _map.SetPlayerPosition(playerId, x, y);

                            PlayerNode playerNode = new(TextureHolder.Get($"Texture/Character/{(PlayerSkin)i}"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                                Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE)
                            };

                            _playerLayer.AttachChild(playerNode);
                            _playerNodes.Add(playerId, playerNode);
                        }
                    }
                    break;
                case ServerMessageType.PlayerMoved: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        Direction direction = Enum.Parse<Direction>(message.Data["d"]);

                        if (x != _map.PlayerPositions[playerId].X || y != _map.PlayerPositions[playerId].Y) {
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
                            _map.PlayerPositions.Remove(playerId);
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
                            _map.RemoveBomb(x, y);
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
                                _bombLayer.AttachChild(new ExplosionNode(TextureHolder.Get("Texture/Effect/Explosion"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                                    Position = new Vector2(Position.FromString(pos).Y * TILE_SIZE, Position.FromString(pos).X * TILE_SIZE)
                                });
                            }
                            _map.RemoveBomb(x, y);
                            _bombLayer.DetachChild(_bombNodes[(x, y)]);
                            _bombNodes.Remove((x, y));
                        }
                    }
                    break;
                case ServerMessageType.PlayerDied: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);

                        lock (_lock) {
                            _map.SetPlayerPosition(playerId, x, y);
                            _playerNodes[playerId].Die();
                            _playerNodes[playerId].TeleportTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), Direction.Down);
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

        private void HandleUpdate(GameTime gameTime) {
            _sceneGraph.UpdateTree(gameTime);

            int playerId = NetworkManager.Instance.ClientId;
            if (_map == null || !_map.PlayerPositions.ContainsKey(playerId) || !_playerNodes.ContainsKey(playerId)) {
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
                    int x = _map.PlayerPositions[playerId].X;
                    int y = _map.PlayerPositions[playerId].Y;
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

                    for (int i = 0; i < _map.Height; i++) {
                        for (int j = 0; j < _map.Width; j++) {
                            lock (_lock) {
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
