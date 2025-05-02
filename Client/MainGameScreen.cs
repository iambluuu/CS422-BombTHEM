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
    public class MainGameScreen : GameScreen {
        private SceneNode _sceneGraph;
        private SceneNode _mapLayer, _bombLayer, _playerLayer;

        private const int TILE_SIZE = 48;

        private Map _map = null;
        private readonly Dictionary<int, PlayerNode> _playerNodes = [];
        private readonly Dictionary<(int, int), SpriteNode> _bombSprites = [];

        public MainGameScreen() { }

        public override void Initialize() {
            base.Initialize();

            _sceneGraph = new SceneNode();
            _mapLayer = new SceneNode();
            _bombLayer = new SceneNode();
            _playerLayer = new SceneNode();
            _sceneGraph.AttachChild(_mapLayer);
            _sceneGraph.AttachChild(_bombLayer);
            _sceneGraph.AttachChild(_playerLayer);
        }

        public override void Activate() {
            base.Activate();
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetGameInfo));
        }

        public override void HandleResponse(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.GameInfo: {
                        _map = Map.FromString(message.Data["map"]);

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

                        int playerCount = int.Parse(message.Data["playerCount"]);
                        int[] playerIds = Array.ConvertAll(message.Data["playerIds"].Split(';'), int.Parse);
                        Position[] playerPositions = Array.ConvertAll(message.Data["playerPositions"].Split(';'), Position.FromString);

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
                        _map.SetPlayerPosition(playerId, x, y);

                        _playerNodes[playerId].MoveTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), direction, 0.2f);
                    }
                    break;
                case ServerMessageType.PlayerLeft: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        _map.PlayerPositions.Remove(playerId);
                        if (_playerNodes.ContainsKey(playerId)) {
                            _playerLayer.DetachChild(_playerNodes[playerId]);
                            _playerNodes.Remove(playerId);
                        }
                    }
                    break;
                case ServerMessageType.BombPlaced: {
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
                case ServerMessageType.BombExploded: {
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
                case ServerMessageType.PlayerDied: {
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

        private void HandleUpdate(GameTime gameTime) {
            _sceneGraph.UpdateTree(gameTime);

            if (_map == null) {
                return;
            }

            int playerId = NetworkManager.Instance.ClientId;
            KeyboardState key = Keyboard.GetState();

            if (true) {
                Direction direction = Direction.None;
                if (_playerNodes.ContainsKey(playerId) && !_playerNodes[playerId].Moving) {
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
                    NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.MovePlayer, new() {
                        { "direction", direction.ToString() }
                    }));
                }
            }

            if (true) {
                if (key.IsKeyDown(Keys.Space)) {
                    NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                        { "type", BombType.Normal.ToString() },
                    }));
                } else if (key.IsKeyDown(Keys.Enter)) {
                    NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                        { "type", BombType.Special.ToString() },
                    }));
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
