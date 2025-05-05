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

        public Bomb(Position position, BombType type, int playerId = -1) {
            PlayerId = playerId;
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

            PlayerPositions[playerId] = new Position(x, y);
        }

        public Position GetPlayerPosition(int playerId) {
            if (!PlayerPositions.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.GetPlayerPosition: Player ID {playerId} not found");
            }

            return PlayerPositions[playerId];
        }

        public bool IsPlayerMovable(int playerId, Direction direction) {
            if (!PlayerPositions.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.IsPlayerMovable: Player ID {playerId} not found");
            }

            Position newPosition = PlayerPositions[playerId].Move(direction);
            return IsInBounds(newPosition.X, newPosition.Y) && GetTile(newPosition.X, newPosition.Y) == TileType.Empty;
        }

        public void MovePlayer(int playerId, Direction direction) {
            if (!PlayerPositions.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.MovePlayer: Player ID {playerId} not found");
            }

            Position newPosition = PlayerPositions[playerId].Move(direction);
            MovePlayer(playerId, newPosition.X, newPosition.Y);
        }

        public void MovePlayer(int playerId, int newX, int newY) {
            if (!PlayerPositions.ContainsKey(playerId)) {
                throw new KeyNotFoundException($"Map.MovePlayer: Player ID {playerId} not found");
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
                throw new ArgumentOutOfRangeException($"Map.HasBomb: Coordinate ({x}, {y}) is out of bounds");
            }

            foreach (var bomb in Bombs) {
                if (bomb.Position.X == x && bomb.Position.Y == y) {
                    return true;
                }
            }

            return false;
        }

        public void AddBomb(int x, int y, BombType bombType, int playerId = -1) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.AddBomb: Coordinate ({x}, {y}) is out of bounds");
            }

            if (HasBomb(x, y)) {
                return;
            }

            Bombs.Add(new Bomb(new Position(x, y), bombType, playerId));
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

                        if (IsInBounds(newX, newY)) {
                            if (GetTile(newX, newY) == TileType.Empty) {
                                bomb.ExplosionPositions.Add(new Position(newX, newY));
                            } else if (GetTile(newX, newY) == TileType.Grass) {
                                bomb.ExplosionPositions.Add(new Position(newX, newY));
                                SetTile(newX, newY, TileType.Empty);
                            } else {
                                break;
                            }
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

                    foreach (var player in PlayerPositions) {
                        if (current.X == player.Value.X && current.Y == player.Value.Y) {
                            final = player.Value;
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
            }
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
    }
}
