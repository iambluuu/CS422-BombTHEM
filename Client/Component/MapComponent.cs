using System.Collections.Generic;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Shared;

namespace Client.Component {
    public class MapComponent : IComponent {
        private readonly object _lock = new();
        public readonly Dictionary<int, PlayerNode> PlayerNodes = [];
        private readonly Dictionary<(int, int), BombNode> BombNodes = [];
        private readonly Dictionary<(int, int), SpriteNode> GrassNodes = [];
        private readonly Dictionary<(int, int), ItemNode> ItemNodes = [];
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
            _sceneGraph.AttachChild(_bombLayer);
            _sceneGraph.AttachChild(_playerLayer);
            _sceneGraph.AttachChild(_ItemLayer);
            _sceneGraph.AttachChild(_vfxLayer);
            _sceneGraph.AttachChild(_pingText);
            _sceneGraph.Position = Position;

            map = mapRenderInfo;
            _pingText = new TextNode("Ping: ?ms") {
                Position = new Vector2(10 * GameValues.TILE_SIZE, 14 * GameValues.TILE_SIZE + 10),
                Color = Color.White,
            };
            _sceneGraph.Position = Position;
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
            UpdateItemNodes();
        }

        private void UpdateBombNodes() {
        }

        private void UpdatePlayerNodes() {

        }

        private void UpdateGrassNodes() {
        }

        private void UpdateItemNodes() {
            lock (_lock) {
                foreach (var (x, y, powerUpType) in map.NewItemDropped) {
                    if (!ItemNodes.ContainsKey((x, y))) {
                        var itemNode = new ItemNode(powerUpType) {
                            Position = new Vector2(x * GameValues.TILE_SIZE, y * GameValues.TILE_SIZE),
                        };

                        ItemNodes.Add((x, y), itemNode);
                        _sceneGraph.AttachChild(itemNode);
                    }
                }
                map.NewItemDropped.Clear();
            }

            lock (_lock) {
                foreach (var (x, y, powerUpType) in map.RemovedItem) {
                    if (ItemNodes.TryGetValue((x, y), out var itemNode)) {
                        _sceneGraph.DetachChild(itemNode);
                        ItemNodes.Remove((x, y));
                    }
                }
                map.RemovedItem.Clear();
            }
        }

        private void ProcessMap() {
            for (int i = 0; i < map.Height; i++) {
                for (int j = 0; j < map.Width; j++) {
                    SpriteNode cellSprite = new(TextureHolder.Get("Tileset/TilesetField", new Rectangle(16, 16, 16, 16)), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE)) {
                        Position = new Vector2(j * GameValues.TILE_SIZE, i *GameValues.TILE_SIZE)
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
                                localArea[u + 1, v + 1] = _map.GetTile(newX, newY) == TileType.Wall;
                            } else {
                                localArea[u + 1, v + 1] = false;
                            }
                        }
                    }

                    (int, int) p = BitmaskReferences.GetPosition(localArea);
                    SpriteNode wallSprite = new(TextureHolder.Get("Tileset/TilesetFloor", new Rectangle(p.Item2 * 16, p.Item1 * 16, 16, 16)), new Vector2(TILE_SIZE,GameValues.TILE_SIZE)) {
                        Position = new Vector2(j * GameValues.TILE_SIZE, i * GameValues.TILE_SIZE)
                    };
                    _mapLayer.AttachChild(wallSprite);
                }
            }
        }
    }
}

