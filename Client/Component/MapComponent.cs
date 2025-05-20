using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

namespace Client.Component {
    public class MapComponent : IComponent {
        private readonly object _lock = new();
        public readonly Dictionary<int, PlayerNode> _playerNodes = [];
        private readonly Dictionary<(int, int), BombNode> _bombNodes = [];
        private readonly Dictionary<(int, int), SpriteNode> _grassNodes = [];
        private readonly Dictionary<(int, int), ItemNode> _itemNodes = [];
        private readonly Dictionary<(int, PowerName), SceneNode> _playerPowerNodes = [];
        private readonly MapRenderInfo map;
        private SceneNode _sceneGraph;
        public SceneNode _mapLayer, _bombLayer, _playerLayer, _ItemLayer, _vfxLayer;
        public TextNode _pingText;

        public override Vector2 Position {
            get => _sceneGraph.Position;
            set => _sceneGraph.Position = value;
        }

        public MapComponent(MapRenderInfo mapRenderInfo) {
            _sceneGraph = new SceneNode();
            _mapLayer = new SceneNode();
            _bombLayer = new SceneNode();
            _playerLayer = new SceneNode();
            _ItemLayer = new SceneNode();
            _vfxLayer = new SceneNode();
            _sceneGraph.AttachChild(_mapLayer);
            _sceneGraph.AttachChild(_ItemLayer);
            _sceneGraph.AttachChild(_bombLayer);
            _sceneGraph.AttachChild(_playerLayer);
            _sceneGraph.AttachChild(_vfxLayer);
            _sceneGraph.Position = Position;

            map = mapRenderInfo;
            _pingText = new TextNode("Ping: ?ms") {
                Position = new Vector2(10 * GameValues.TILE_SIZE, 14 * GameValues.TILE_SIZE + 10),
                Color = Color.White,
            };
            _sceneGraph.AttachChild(_pingText);
            _sceneGraph.Position = Position;
        }

        public Vector2 GetPlayerSpritePosition(int playerId) {
            if (_playerNodes.TryGetValue(playerId, out var playerNode)) {
                return playerNode.Position;
            }
            return Vector2.Zero;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            base.Draw(spriteBatch);
            _sceneGraph.DrawTree(spriteBatch, Matrix.Identity);
        }

        public override void Update(GameTime gameTime) {
            _sceneGraph.UpdateTree(gameTime);
            _pingText.Text = $"Ping: {NetworkManager.Instance.Ping}ms";
            if (NetworkManager.Instance.Ping > 200) {
                _pingText.Color = Color.Red;
            } else {
                _pingText.Color = Color.White;
            }

            UpdateBombNodes();
            UpdatePlayerNodes();
            UpdateGrassNodes();
            UpdatePowerUpNodes();
            UpdateItemNodes();
        }

        private void UpdateBombNodes() {
            lock (_lock) {
                var newBombs = map.FlushNewBomb();
                foreach (var (x, y, bombType) in newBombs) {
                    if (!_bombNodes.ContainsKey((x, y))) {
                        var bombNode = BombNodeFactory.CreateNode(bombType);
                        bombNode.Position = new Vector2(y * GameValues.TILE_SIZE, x * GameValues.TILE_SIZE);

                        _bombNodes.Add((x, y), bombNode);
                        _bombLayer.AttachChild(bombNode);
                    }
                }
            }

            lock (_lock) {
                var removedBombs = map.FlushRemovedBomb();
                foreach (var (x, y) in removedBombs) {
                    if (_bombNodes.TryGetValue((x, y), out var bombNode)) {
                        _bombLayer.DetachChild(bombNode);
                        _bombNodes.Remove((x, y));
                    }
                }
            }

            lock (_lock) {
                var explodedBombs = map.FlushNewExplosion();
                foreach (var (x, y) in explodedBombs) {
                    _bombLayer.AttachChild(new ExplosionNode(TextureHolder.Get("Effect/Explosion"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE)) {
                        Position = new Vector2(y * GameValues.TILE_SIZE, x * GameValues.TILE_SIZE)
                    });
                }
            }
        }

        private void UpdatePlayerNodes() {
            lock (_lock) {
                var DeadPlayers = map.FlushDeadPlayers();
                foreach (var playerId in DeadPlayers) {
                    if (_playerNodes.TryGetValue(playerId, out var playerNode)) {
                        playerNode.Die();
                    }
                }
            }

            lock (_lock) {
                var movedPlayers = map.FlushMovedPlayers();
                foreach (var (playerId, x, y, direction) in movedPlayers) {
                    if (_playerNodes.TryGetValue(playerId, out var playerNode)) {
                        playerNode.MoveTo(new Vector2(y * GameValues.TILE_SIZE, x * GameValues.TILE_SIZE), direction);
                    }
                }
            }

            lock (_lock) {
                var teleportedPlayers = map.FlushTeleportedPlayers();
                foreach (var (playerId, x, y) in teleportedPlayers) {
                    if (_playerNodes.TryGetValue(playerId, out var playerNode)) {
                        playerNode.TeleportTo(new Vector2(y * GameValues.TILE_SIZE, x * GameValues.TILE_SIZE), Direction.Down);
                    }
                }
            }

            lock (_lock) {
                var removedPlayers = map.FlushRemovedPlayers();
                foreach (var playerId in removedPlayers) {
                    if (_playerNodes.TryGetValue(playerId, out var playerNode)) {
                        _playerLayer.DetachChild(playerNode);
                        _playerNodes.Remove(playerId);
                    }
                }
            }
        }

        private void UpdateGrassNodes() {
            lock (_lock) {
                var destroyedGrass = map.FlushDestroyedBlock();
                foreach (var (x, y) in destroyedGrass) {
                    if (_grassNodes.TryGetValue((x, y), out var grassNode)) {
                        _bombLayer.DetachChild(grassNode);
                        _grassNodes.Remove((x, y));
                    }
                }
            }
        }

        private void UpdatePowerUpNodes() {
            lock (_lock) {
                var newPowerUps = map.FlushNewEnvVFX();
                foreach (var (x, y, powerUpType) in newPowerUps) {
                    var vfx = EffectNodeFactory.CreateEnvEffect(powerUpType, x, y);
                    _vfxLayer.AttachChild(vfx);
                }
            }

            lock (_lock) {
                var newPowerUps = map.FlushNewPlayerVFX();
                foreach (var (playerId, powerUpType) in newPowerUps) {
                    if (!_playerPowerNodes.ContainsKey((playerId, powerUpType))) {
                        var vfx = EffectNodeFactory.CreatePlayerEffect(powerUpType);
                        _playerNodes[playerId].AttachChild(vfx);
                        if (powerUpType != PowerName.Teleport) {
                            _playerPowerNodes.Add((playerId, powerUpType), vfx);
                        }
                    }
                }

                lock (_lock) {
                    var removedPowerUps = map.FlushExpiredPlayerPowerUp();
                    foreach (var (playerId, powerName) in removedPowerUps) {
                        if (_playerPowerNodes.TryGetValue((playerId, powerName), out var vfx)) {
                            _playerPowerNodes.Remove((playerId, powerName));
                            _playerNodes[playerId].DetachChild(vfx);
                        }
                    }
                }
            }
        }

        private void UpdateItemNodes() {
            lock (_lock) {
                var newItems = map.FlushNewItemDropped();
                foreach (var (x, y, powerUpType) in newItems) {
                    if (!_itemNodes.ContainsKey((x, y))) {
                        var itemNode = new ItemNode(powerUpType) {
                            Position = new Vector2(y * GameValues.TILE_SIZE, x * GameValues.TILE_SIZE),
                        };

                        _itemNodes.Add((x, y), itemNode);
                        _ItemLayer.AttachChild(itemNode);
                    }
                }
            }

            lock (_lock) {
                var removedItems = map.FlushRemovedItem();
                foreach (var (x, y) in removedItems) {
                    if (_itemNodes.TryGetValue((x, y), out var itemNode)) {
                        _itemNodes.Remove((x, y));
                        _ItemLayer.DetachChild(itemNode);
                    }
                }
            }
        }

        public void ProcessMap() {
            // Map
            lock (_lock) {
                Reset();

                for (int i = 0; i < map.Height; i++) {
                    for (int j = 0; j < map.Width; j++) {
                        SpriteNode cellSprite = new(TextureHolder.Get("Tileset/TilesetField", new Rectangle(16, 16, 16, 16)), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE)) {
                            Position = new Vector2(j * GameValues.TILE_SIZE, i * GameValues.TILE_SIZE)
                        };
                        _mapLayer.AttachChild(cellSprite);

                        if (map.GetTile(i, j) == TileType.Grass) {
                            SpriteNode grassSprite = new(TextureHolder.Get("Tileset/TilesetNature", new Rectangle(96, 240, 16, 16)), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE)) {
                                Position = new Vector2(j * GameValues.TILE_SIZE, i * GameValues.TILE_SIZE)
                            };

                            _grassNodes.Add((i, j), grassSprite);
                            _bombLayer.AttachChild(grassSprite);
                        }

                        if (map.GetTile(i, j) != TileType.Wall) {
                            continue;
                        }

                        bool[,] localArea = new bool[3, 3];
                        for (int u = -1; u <= 1; u++) {
                            for (int v = -1; v <= 1; v++) {
                                int newX = i + u;
                                int newY = j + v;
                                if (map.IsInBounds(newX, newY)) {
                                    localArea[u + 1, v + 1] = map.GetTile(newX, newY) == TileType.Wall;
                                } else {
                                    localArea[u + 1, v + 1] = false;
                                }
                            }
                        }

                        (int, int) p = BitmaskReferences.GetPosition(localArea);
                        SpriteNode wallSprite = new(TextureHolder.Get("Tileset/TilesetFloor", new Rectangle(p.Item2 * 16, p.Item1 * 16, 16, 16)), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE)) {
                            Position = new Vector2(j * GameValues.TILE_SIZE, i * GameValues.TILE_SIZE)
                        };
                        _mapLayer.AttachChild(wallSprite);
                    }
                }

                // Player
                foreach (var info in map.PlayerInfos) {
                    var playerId = info.Key;
                    var playerInfo = info.Value;
                    PlayerNode playerNode = new(TextureHolder.Get($"Character/{playerInfo.SkinId}"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE)) {
                        Position = new Vector2(playerInfo.Position.Y * GameValues.TILE_SIZE, playerInfo.Position.X * GameValues.TILE_SIZE)
                    };
                    _playerNodes[playerId] = playerNode;
                    _playerLayer.AttachChild(playerNode);
                }

                map.MyNode = _playerNodes[NetworkManager.Instance.ClientId];
            }
        }

        private void Reset() {
            _bombNodes.Clear();
            _grassNodes.Clear();
            _itemNodes.Clear();
            _playerPowerNodes.Clear();
            _playerNodes.Clear();

            _mapLayer.DetachAllChildren();
            _bombLayer.DetachAllChildren();
            _playerLayer.DetachAllChildren();
            _ItemLayer.DetachAllChildren();
            _vfxLayer.DetachAllChildren();
        }

        public bool IsPlayerMoving(int playerId) {
            lock (_lock) {
                if (_playerNodes.TryGetValue(playerId, out var playerNode)) {
                    return playerNode.Moving;
                }
                return false;
            }
        }

        public bool ContainsPlayer(int playerId) {
            lock (_lock) {
                return _playerNodes.ContainsKey(playerId);
            }
        }

        public void SetDirection(int playerId, Direction direction) {
            lock (_lock) {
                if (_playerNodes.TryGetValue(playerId, out var playerNode)) {
                    playerNode.SetDirection(direction);
                }
            }
        }
    }
}

