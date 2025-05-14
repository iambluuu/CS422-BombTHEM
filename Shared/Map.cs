using Server;

namespace Shared {
    public enum PowerName {
        None,
        Ghost,
        MoreBombs,
        InstantBomb,
        Shield,
        Teleport,
    }

    public enum TileType {
        Empty,
        Wall,
        Grass,
    }

    public enum BombType {
        Normal,
        Special,
    }

    public enum PlayerSkin {
        NinjaBlue,
        NinjaGreen,
        NinjaRed,
        NinjaYellow,
    }

    public enum Direction {
        Down,
        Up,
        Left,
        Right,
        None,
    }

    public class Position {
        public int X { get; set; }
        public int Y { get; set; }

        public Position(int x, int y) {
            X = x;
            Y = y;
        }

        public Position Move(Direction direction) {
            Position newPosition = new Position(X, Y);
            switch (direction) {
                case Direction.Down:
                    newPosition.X++;
                    break;
                case Direction.Up:
                    newPosition.X--;
                    break;
                case Direction.Left:
                    newPosition.Y--;
                    break;
                case Direction.Right:
                    newPosition.Y++;
                    break;
            }

            return newPosition;
        }

        public override bool Equals(object? obj) {
            if (obj == null) return false;

            if (obj is Position other)
                return this.X == other.X && this.Y == other.Y;
            return false;
        }

        public override int GetHashCode() {
            // Combine X and Y into a unique hash
            return HashCode.Combine(X, Y);
        }

        public override string ToString() {
            return $"({X}, {Y})";
        }

        public static Position FromString(string str) {
            string[] parts = str.Trim('(', ')').Split(',');
            if (parts.Length != 2) {
                throw new FormatException("Invalid position format");
            }

            return new Position(int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }

    public class Bomb {
        public int PlayerId { get; set; }
        public Position Position { get; set; }
        public BombType Type { get; set; }
        public DateTime PlaceTime { get; set; }
        public DateTime ExplodeTime { get; set; }
        public List<Position> ExplosionPositions { get; set; }
        public bool IsCounted { get; set; } = true; // Indicates if the bomb is counted for the player Bomb count

        public Bomb(Position position, BombType type, int playerId = -1, bool isCounted = true) {
            PlayerId = playerId;
            Position = position;
            Type = type;
            PlaceTime = DateTime.Now;
            ExplodeTime = DateTime.MinValue;
            ExplosionPositions = new List<Position>();
            IsCounted = isCounted;
        }
    }

    public class DroppedItem {
        public DateTime DropTime { get; set; }
        public Position Position { get; set; }
        public PowerName Item { get; set; }

        public DroppedItem(Position position, PowerName item) {
            Position = position;
            Item = item;
            DropTime = DateTime.Now;
        }
    }

    public class ActivePowerUp {
        public PowerName PowerType { get; set; }
        public DateTime StartTime { get; set; }

        public ActivePowerUp(PowerName name, DateTime startTime) {
            PowerType = name;
            StartTime = startTime;
        }
    }

    public class Map {
        public int Height { get; private set; }
        public int Width { get; private set; }
        public TileType[,] Tiles { get; private set; }
        public Dictionary<int, PlayerIngameInfo> PlayerInfos { get; private set; }
        public List<Bomb> Bombs { get; private set; }
        public List<DroppedItem> Items { get; private set; }

        public Map(int height, int width) {
            Height = height;
            Width = width;
            Tiles = new TileType[height, width];
            PlayerInfos = [];
            Bombs = new List<Bomb>();
            Items = new List<DroppedItem>();
        }

        public bool IsInBounds(int x, int y) {
            return x >= 0 && x < Height && y >= 0 && y < Width;
        }

        public void SetTile(int x, int y, TileType tileType) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.SetTile: Coordinate ({x}, {y}) is out of bounds");
            }

            Tiles[x, y] = tileType;
        }

        public TileType GetTile(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.GetTile: Coordinate ({x}, {y}) is out of bounds");
            }

            return Tiles[x, y];
        }


        public void SetPlayerPosition(int playerId, int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.SetPlayerPosition: Coordinate ({x}, {y}) is out of bounds");
            }
            if (!PlayerInfos.ContainsKey(playerId)) {
                PlayerInfos.Add(playerId, new PlayerIngameInfo(playerId.ToString(), new Position(x, y)));
            } else {
                PlayerInfos[playerId].Position = new Position(x, y);
            }
        }

        public Position GetPlayerPosition(int playerId) {
            if (!PlayerInfos.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.GetPlayerPosition: Player ID {playerId} not found");
            }

            return PlayerInfos[playerId].Position;
        }

        public bool IsPlayerMovable(int playerId, Direction direction) {
            if (!PlayerInfos.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.IsPlayerMovable: Player ID {playerId} not found");
            }

            Position newPosition = PlayerInfos[playerId].Position.Move(direction);
            return IsInBounds(newPosition.X, newPosition.Y) && GetTile(newPosition.X, newPosition.Y) == TileType.Empty;
        }

        public void MovePlayer(int playerId, Direction direction) {
            if (!PlayerInfos.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.MovePlayer: Player ID {playerId} not found");
            }

            Position newPosition = PlayerInfos[playerId].Position.Move(direction);
            MovePlayer(playerId, newPosition.X, newPosition.Y);
        }

        public void MovePlayer(int playerId, int newX, int newY) {
            if (!PlayerInfos.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.MovePlayer: Player ID {playerId} not found");
            }

            if (!IsInBounds(newX, newY)) {
                return;
            }

            if (GetTile(newX, newY) != TileType.Empty) {
                return;
            }

            if (PlayerInfos.ContainsKey(playerId)) {
                PlayerInfos[playerId].Position = new Position(newX, newY);
            } else {
                PlayerInfos.Add(playerId, new PlayerIngameInfo(playerId.ToString(), new Position(newX, newY)));
            }
        }

        public bool HasBomb(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.HasBomb: Coordinate ({x}, {y}) is out of bounds");
            }

            foreach (var bomb in Bombs) {
                if (bomb.Position.X == x && bomb.Position.Y == y) {
                    return true;
                }
            }

            return false;
        }

        public bool AddBomb(int x, int y, BombType bombType, int playerId = -1) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.AddBomb: Coordinate ({x}, {y}) is out of bounds");
            }

            if (HasBomb(x, y) || GetTile(x, y) != TileType.Empty) {
                return false;
            }

            var byPlayer = PlayerInfos.TryGetValue(playerId, out PlayerIngameInfo? player) ? player : null;
            if (byPlayer != null && !byPlayer.CanPlaceBomb()) {
                return false;
            }

            bool isCounted = byPlayer != null && !byPlayer.HasPowerUp(PowerName.MoreBombs);
            Bombs.Add(new Bomb(new Position(x, y), bombType, playerId, isCounted));
            return true;
        }

        public void RemoveBomb(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.RemoveBomb: Coordinate ({x}, {y}) is out of bounds");
            }

            int bombId = Bombs.FindIndex(b => b.Position.X == x && b.Position.Y == y);
            if (bombId < 0) {
                throw new KeyNotFoundException($"Map.RemoveBomb: No bomb found at ({x}, {y})");
            }

            RemoveBomb(bombId);
        }

        public void RemoveBomb(int bombId) {
            if (bombId < 0 || bombId >= Bombs.Count) {
                throw new ArgumentOutOfRangeException($"Map.RemoveBomb: Bomb ID {bombId} is out of bounds");
            }

            Bomb targetBomb = Bombs[bombId];
            if (targetBomb.IsCounted) {
                PlayerInfos[targetBomb.PlayerId].DecreaseBombCount();
            }
            Bombs.RemoveAt(bombId);
        }

        public void ExplodeBomb(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.ExplodeBomb: Coordinate ({x}, {y}) is out of bounds");
            }

            int bombId = Bombs.FindIndex(b => b.Position.X == x && b.Position.Y == y);
            if (bombId < 0) {
                throw new KeyNotFoundException($"Map.ExplodeBomb: No bomb found at ({x}, {y})");
            }

            ExplodeBomb(bombId);
        }

        public void ExplodeBomb(int bombId) {
            if (bombId < 0 || bombId >= Bombs.Count) {
                throw new ArgumentOutOfRangeException($"Map.ExplodeBomb: Bomb ID {bombId} is out of bounds");
            }

            var bomb = Bombs[bombId];
            if (bomb.ExplosionPositions.Count > 0) {
                throw new InvalidOperationException($"Map.ExplodeBomb: Bomb {bombId} has already exploded");
            }

            List<Position> directions = [
                new(0, -1),
                new(0, 1),
                new(-1, 0),
                new(1, 0),
            ];

            if (bomb.Type == BombType.Normal) {
                bomb.ExplosionPositions.Add(bomb.Position);
                foreach (var direction in directions) {
                    for (int i = 1; i <= 2; i++) {
                        int newX = bomb.Position.X + direction.X * i;
                        int newY = bomb.Position.Y + direction.Y * i;

                        if (IsInBounds(newX, newY) && (GetTile(newX, newY) == TileType.Empty || GetTile(newX, newY) == TileType.Grass)) {
                            bomb.ExplosionPositions.Add(new Position(newX, newY));
                        } else {
                            break;
                        }
                    }
                }
            } else if (bomb.Type == BombType.Special) {
                Queue<Position> queue = new();
                bool[,] visited = new bool[Height, Width];
                Position[,] previous = new Position[Height, Width];
                queue.Enqueue(bomb.Position);
                visited[bomb.Position.X, bomb.Position.Y] = true;

                Position? final = null;
                while (queue.Count > 0) {
                    Position current = queue.Dequeue();

                    foreach (var player in PlayerInfos) {
                        if (current.X == player.Value.Position.X && current.Y == player.Value.Position.Y) {
                            final = player.Value.Position;
                            break;
                        }
                    }

                    // if (final != null) {
                    //     break;
                    // }

                    foreach (var direction in Utils.Shuffle(directions)) {
                        int newX = current.X + direction.X;
                        int newY = current.Y + direction.Y;

                        if (IsInBounds(newX, newY) && GetTile(newX, newY) != TileType.Wall && !visited[newX, newY]) {
                            visited[newX, newY] = true;
                            previous[newX, newY] = current;
                            queue.Enqueue(new Position(newX, newY));
                        }
                    }
                }

                if (final != null) {
                    Position current = final;
                    while (current.X != bomb.Position.X || current.Y != bomb.Position.Y) {
                        bomb.ExplosionPositions.Add(current);
                        current = previous[current.X, current.Y];
                    }
                    bomb.ExplosionPositions.Add(bomb.Position);
                } else {
                    bomb.ExplosionPositions.Add(bomb.Position);
                }

                // for (int i = 0; i < bomb.ExplosionPositions.Count; i++) {
                //     SetTile(bomb.ExplosionPositions[i].X, bomb.ExplosionPositions[i].Y, TileType.Empty);
                // }
            }
        }

        public PowerName PickUpItem(int playerId, int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.PickUpItem: Coordinate ({x}, {y}) is out of bounds");
            }

            if (!PlayerInfos.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.PickUpItem: Player ID {playerId} not found");
            }

            int itemId = Items.FindIndex(i => i.Position.X == x && i.Position.Y == y);
            if (itemId < 0) {
                return PowerName.None; // No item found at the specified position
            }

            PowerName powerName = Items[itemId].Item;
            bool picked = PlayerInfos[playerId].PickUpItem(powerName);
            if (picked) {
                RemoveItem(x, y);
                return powerName; // Item picked up successfully
            }

            return PowerName.None; // Player's inventory is full
        }

        public bool HasItem(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.HasItem: Coordinate ({x}, {y}) is out of bounds");
            }

            foreach (var item in Items) {
                if (item.Position.X == x && item.Position.Y == y) {
                    return true;
                }
            }

            return false;
        }

        public void AddItem(int x, int y, PowerName item) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.AddItem: Coordinate ({x}, {y}) is out of bounds");
            }

            if (GetTile(x, y) != TileType.Empty) {
                return;
            }

            Items.Add(new DroppedItem(new Position(x, y), item));
        }

        public void RemoveItem(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.RemoveItem: Coordinate ({x}, {y}) is out of bounds");
            }

            int itemId = Items.FindIndex(i => i.Position.X == x && i.Position.Y == y);
            if (itemId < 0) {
                throw new KeyNotFoundException($"Map.RemoveItem: No item found at ({x}, {y})");
            }

            Items.RemoveAt(itemId);
        }

        public bool UsePowerUp(int playerId, PowerName power) {
            if (!PlayerInfos.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.UsePowerUp: Player ID {playerId} not found");
            }

            return PlayerInfos[playerId].UsePowerUp(power);
        }

        public override string ToString() {
            string mapString = "";
            for (int i = 0; i < Height; i++) {
                for (int j = 0; j < Width; j++) {
                    mapString += ((int)Tiles[i, j]).ToString();
                }
                if (i < Height - 1) mapString += ";";
            }

            return mapString;
        }

        public static Map FromString(string str) {
            string[] rows = str.Split(';');
            int height = rows.Length;
            int width = rows[0].Length;
            Map map = new(height, width);
            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    map.SetTile(i, j, Enum.Parse<TileType>(rows[i][j].ToString()));
                }
            }

            return map;
        }

        public Position GetSafePosition(int playerId) {
            Position newPos = new Position(0, 0);
            while (true) {
                newPos.X = Utils.RandomInt(Height);
                newPos.Y = Utils.RandomInt(Width);
                if (IsInBounds(newPos.X, newPos.Y) && GetTile(newPos.X, newPos.Y) == TileType.Empty) {
                    return newPos;
                }
            }
        }
    }
}
