namespace Shared {
    public enum PowerName : byte {
        None,
        MoreBombs,
        Nuke,
        Shield,
        Teleport,
    }

    public enum TileType : byte {
        Empty,
        Wall,
        Grass,
    }

    public enum BombType : byte {
        Normal,
        Special,
        Nuke
    }

    public enum PlayerSkin : byte {
        NinjaBlue,
        NinjaGreen,
        NinjaRed,
        NinjaYellow,
    }

    public enum Direction : byte {
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
        public int SlotNum { get; set; } = -1;

        public ActivePowerUp(PowerName name, DateTime startTime, int slotNum = -1) {
            PowerType = name;
            StartTime = startTime;
            SlotNum = slotNum;
        }
    }
}
