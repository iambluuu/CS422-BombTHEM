namespace Shared {
    public enum TileType {
        Empty,
        Wall,
        Grass,
    }

    public enum BombType {
        Normal,
        Special,
    }

    public enum Direction {
        Up,
        Down,
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
                case Direction.Up:
                    newPosition.X--;
                    break;
                case Direction.Down:
                    newPosition.X++;
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
        public Position Position { get; set; }
        public BombType Type { get; set; }
        public DateTime PlaceTime { get; set; }
        public DateTime ExplodeTime { get; set; }
        public List<Position> ExplosionPositions { get; set; }

        public Bomb(Position position, BombType type) {
            Position = position;
            Type = type;
            PlaceTime = DateTime.Now;
            ExplodeTime = DateTime.MinValue;
            ExplosionPositions = new List<Position>();
        }
    }

    public class Map {
        public int Height { get; private set; }
        public int Width { get; private set; }
        public TileType[,] Tiles { get; private set; }
        public Dictionary<int, Position> PlayerPositions { get; private set; }
        public List<Bomb> Bombs { get; private set; }

        public Map(int height, int width) {
            Height = height;
            Width = width;
            Tiles = new TileType[height, width];
            PlayerPositions = new Dictionary<int, Position>();
            Bombs = new List<Bomb>();
        }

        public bool IsInBounds(int x, int y) {
            return x >= 0 && x < Height && y >= 0 && y < Width;
        }

        public void SetTile(int x, int y, TileType tileType) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Coordinate ({x}, {y}) is out of bounds");
            }

            Tiles[x, y] = tileType;
        }

        public TileType GetTile(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Coordinate ({x}, {y}) is out of bounds");
            }

            return Tiles[x, y];
        }

        public void SetPlayerPosition(int playerId, int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Coordinate ({x}, {y}) is out of bounds");
            }

            PlayerPositions[playerId] = new Position(x, y);
        }

        public Position GetPlayerPosition(int playerId) {
            if (!PlayerPositions.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Player ID {playerId} not found");
            }

            return PlayerPositions[playerId];
        }

        public void MovePlayer(int playerId, Direction direction) {
            if (!PlayerPositions.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Player ID {playerId} not found");
            }

            Position newPosition = PlayerPositions[playerId].Move(direction);
            MovePlayer(playerId, newPosition.X, newPosition.Y);
        }

        public void MovePlayer(int playerId, int newX, int newY) {
            if (!PlayerPositions.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Player ID {playerId} not found");
            }

            if (!IsInBounds(newX, newY)) {
                return;
            }

            if (GetTile(newX, newY) != TileType.Empty) {
                return;
            }

            PlayerPositions[playerId] = new Position(newX, newY);
        }

        public bool HasBomb(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Coordinate ({x}, {y}) is out of bounds");
            }

            foreach (var bomb in Bombs) {
                if (bomb.Position.X == x && bomb.Position.Y == y) {
                    return true;
                }
            }

            return false;
        }

        public void AddBomb(int x, int y, BombType bombType) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Coordinate ({x}, {y}) is out of bounds");
            }

            if (HasBomb(x, y)) {
                return;
            }

            Bombs.Add(new Bomb(new Position(x, y), bombType));
        }

        public void RemoveBomb(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Coordinate ({x}, {y}) is out of bounds");
            }

            int bombId = Bombs.FindIndex(b => b.Position.X == x && b.Position.Y == y);
            if (bombId < 0) {
                throw new KeyNotFoundException($"No bomb found at ({x}, {y})");
            }

            RemoveBomb(bombId);
        }

        public void RemoveBomb(int bombId) {
            if (bombId < 0 || bombId >= Bombs.Count) {
                throw new ArgumentOutOfRangeException($"Bomb ID {bombId} is out of bounds");
            }

            Bombs.RemoveAt(bombId);
        }

        public void ExplodeBomb(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Coordinate ({x}, {y}) is out of bounds");
            }

            int bombId = Bombs.FindIndex(b => b.Position.X == x && b.Position.Y == y);
            if (bombId < 0) {
                throw new KeyNotFoundException($"No bomb found at ({x}, {y})");
            }

            ExplodeBomb(bombId);
        }

        public void ExplodeBomb(int bombId) {
            if (bombId < 0 || bombId >= Bombs.Count) {
                throw new ArgumentOutOfRangeException($"Bomb ID {bombId} is out of bounds");
            }

            var bomb = Bombs[bombId];
            if (bomb.ExplosionPositions.Count > 0) {
                throw new InvalidOperationException($"Bomb {bombId} has already exploded");
            }

            switch (bomb.Type) {
                case BombType.Normal:
                    List<Position> directions = new List<Position> {
                        new Position(0, -1), // Up
                        new Position(0, 1), // Down
                        new Position(-1, 0), // Left
                        new Position(1, 0), // Right
                    };

                    bomb.ExplosionPositions.Add(bomb.Position);
                    foreach (var direction in directions) {
                        int newX = bomb.Position.X + direction.X;
                        int newY = bomb.Position.Y + direction.Y;

                        if (IsInBounds(newX, newY)) {
                            if (GetTile(newX, newY) == TileType.Empty) {
                                bomb.ExplosionPositions.Add(new Position(newX, newY));
                            } else if (GetTile(newX, newY) == TileType.Grass) {
                                bomb.ExplosionPositions.Add(new Position(newX, newY));
                                SetTile(newX, newY, TileType.Empty);
                            }
                        }
                    }
                    break;
                case BombType.Special:
                    break;
            }
        }
    }
}
