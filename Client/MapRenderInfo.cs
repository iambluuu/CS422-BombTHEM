

using System;
using System.Collections.Generic;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Shared;

namespace Client {
    public class MapRenderInfo {
        private readonly object _lock = new();
        public Dictionary<int, int> SkinMapping = [];
        private Dictionary<int, Position> PlayerPosition = new();
        private readonly List<(int, int)> BombPosition = new();
        private readonly List<(int, int, BombType)> NewBomb = new();
        private readonly List<(int, int)> RemovedBomb = new();
        private readonly List<(int, int, PowerName)> NewItemDropped = new();
        private readonly List<(int, int, PowerName)> RemovedItem = new();

        public int Width { get; }
        public int Height { get; }

        public void PowerUpSpawned(int x, int y, PowerName powerUpType) {
            lock (_lock) {
                NewItemDropped.Add((x, y, powerUpType));
            }
        }

        public void PowerUpRemoved(int x, int y, PowerName powerUpType) {
            lock (_lock) {
                RemovedItem.Add((x, y, powerUpType));
            }
        }

        internal void SetPlayerPosition(int playerId, int x, int y) {
            lock (_lock) {
                PlayerPosition[playerId] = new Position(x, y);
            }
        }

        public TileType GetTile(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.GetTile: Coordinate ({x}, {y}) is out of bounds");
            }

            return Tiles[x, y];
        }

        private bool IsInBounds(int x, int y) {
            throw new NotImplementedException();
        }

        public List<(int, int, PowerName)> FlushNewItemDropped() {
            lock (_lock) {
                var items = new List<(int, int, PowerName)>(NewItemDropped);
                NewItemDropped.Clear();
                return items;
            }
        }

        public List<(int, int, PowerName)> FlushRemovedItem() {
            lock (_lock) {
                var items = new List<(int, int, PowerName)>(RemovedItem);
                RemovedItem.Clear();
                return items;
            }
        }

        public List<(int, int)> FlushRemovedBomb() {
            lock (_lock) {
                var bombs = new List<(int, int)>(BombPosition);
                BombPosition.Clear();
                return bombs;
            }
        }

        public List<(int, int, BombType)> FlushNewBomb() {
            lock (_lock) {
                var bombs = new List<(int, int, BombType)>(NewBomb);
                BombPosition.Clear();
                return bombs;
            }
        }

        public void BombPlaced(int x, int y, BombType bombType) {
            lock (_lock) {
                NewBomb.Add((x, y, bombType));
            }
        }

        public void BombRemoved(int x, int y) {
            lock (_lock) {
                RemovedBomb.Add((x, y));
            }
        }
    }
}