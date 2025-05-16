

using System;
using System.Collections.Generic;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Shared;

namespace Client {
    public class PlayerInfo {
        public Position Position;
        public int Score;
        public readonly PlayerSkin SkinId;
        public readonly string Name;

        public PlayerInfo(int skinId, string name, Position position) {
            SkinId = (PlayerSkin)skinId;
            Name = name;
            Position = position;
        }
    }

    public class MapRenderInfo {
        private readonly object _lock = new();
        public int Duration { get; private set; }

        public bool IsInitialized { get; private set; } = false;
        public Action OnMapInitialized;

        private TileType[,] Tiles;
        private bool[,] tileLocked;
        public Dictionary<int, int> SkinMapping = [];
        public Dictionary<int, PlayerInfo> PlayerInfos { get; private set; } = [];
        public (PowerName, int)[] PowerUps { get; private set; } = [(PowerName.None, 0), (PowerName.None, 0)];
        private bool[] powerUpLocked = [false, false];
        public int BombCount { get; private set; } = 0;

        private readonly List<(int, int)> BombPosition = new();
        private readonly List<(int, int, BombType)> NewBomb = new();
        private readonly List<(int, int)> NewExplosion = new();
        private readonly List<(int, int)> DestroyedBlock = new();
        private readonly List<(int, int)> RemovedBomb = new();
        private readonly List<(int, int, PowerName)> NewItemDropped = new();
        private readonly List<(int, int)> RemovedItem = new();
        private readonly List<(int, PowerName)> ExpiredPowerUp;
        private readonly List<(int, PowerName)> NewPlayerVFX = new();
        private readonly List<((int, int), PowerName)> NewEnvVFX = new();
        private readonly List<(int, int)> ExpiredItem;

        public List<(int, int)> FlushRemovedItem() => FlushList(RemovedItem);
        public List<(int, int)> FlushRemovedBomb() => FlushList(RemovedBomb);
        public List<(int, int, BombType)> FlushNewBomb() => FlushList(NewBomb);
        public List<(int, int)> FlushNewExplosion() => FlushList(NewExplosion);
        public List<(int, int)> FlushDestroyedBlock() => FlushList(DestroyedBlock);
        public List<(int, int, PowerName)> FlushNewItemDropped() => FlushList(NewItemDropped);
        public List<(int, PowerName)> FlushExpiredPowerUp() => FlushList(ExpiredPowerUp);
        public List<(int, PowerName)> FlushActivePowerUp() => FlushList(ActivePowerUp);
        public List<(int, int)> FlushExpiredItem() => FlushList(ExpiredItem);

        internal List<T> FlushList<T>(List<T> list) {
            lock (_lock) {
                var flushed = new List<T>(list);
                list.Clear();
                return flushed;
            }
        }

        public int Width { get; }
        public int Height { get; }

        public void ItemSpawned(int x, int y, PowerName powerUpType) {
            lock (_lock) {
                NewItemDropped.Add((x, y, powerUpType));
            }
        }

        public void PowerUpRemoved(int x, int y, PowerName powerUpType) {
            lock (_lock) {
                RemovedItem.Add((x, y));
            }
        }

        public bool PlayerAt(int playerId, int x, int y) {
            lock (_lock) {
                return PlayerInfos[playerId].Position.X == x && PlayerInfos[playerId].Position.Y == y;
            }
        }

        internal void SetPlayerPosition(int playerId, int x, int y) {
            lock (_lock) {
                PlayerInfos[playerId].Position = new Position(x, y);
            }
        }

        public TileType GetTile(int x, int y) {
            if (!IsInBounds(x, y)) {
                throw new ArgumentOutOfRangeException($"Map.GetTile: Coordinate ({x}, {y}) is out of bounds");
            }

            return Tiles[x, y];
        }

        public bool IsInBounds(int x, int y) {
            return x >= 0 && x < Height && y >= 0 && y < Width;
        }

        public void BombPlaced(int x, int y, BombType bombType, int byPlayerId, bool isCounted) {
            lock (_lock) {
                NewBomb.Add((x, y, bombType));
                BombPosition.Add((x, y));
                if (byPlayerId == NetworkManager.Instance.ClientId && isCounted) {
                    BombCount++;
                }
            }
        }

        public void BombExploded(int x, int y, string[] positions, int byPlayerId, bool isCounted) {
            lock (_lock) {
                RemovedBomb.Add((x, y));
                BombPosition.Remove((x, y));
                foreach (var pos in positions) {
                    int ex = Position.FromString(pos).X;
                    int ey = Position.FromString(pos).Y;
                    NewExplosion.Add((ex, ey));

                    if (Tiles[x, y] == TileType.Grass) {
                        Tiles[x, y] = TileType.Empty;
                        DestroyedBlock.Add((x, y));
                    }
                }

                if (byPlayerId == NetworkManager.Instance.ClientId && isCounted) {
                    BombCount--;
                }
            }
        }

        public void PowerUpExpired(int playerId, PowerName powerUpType) {
            lock (_lock) {
                ExpiredPowerUp.Add((playerId, powerUpType));
            }
        }

        public void ItemExpired(int x, int y) {
            lock (_lock) {
                ExpiredItem.Add((x, y));
            }
        }

        public List<int> GetScores() {
            lock (_lock) {
                var scores = new List<int>();
                foreach (var playerInfo in PlayerInfos.Values) {
                    scores.Add(playerInfo.Score);
                }
                return scores;
            }
        }

        public bool LockPowerSlot(int slotNum) {
            lock (_lock) {
                if (powerUpLocked[slotNum]) {
                    return false;
                }
                powerUpLocked[slotNum] = true;
                return true;
            }
        }

        public void UnlockPowerSlot(int slotNum) {
            lock (_lock) {
                powerUpLocked[slotNum] = false;
            }
        }

        public void IncreaseScore(int playerId, int score) {
            lock (_lock) {
                PlayerInfos[playerId].Score += score;
            }
        }

        public void InitMap(string mapString, int[] playerIds, Position[] playerPositions, string[] playerNames, int duration) {
            if (playerIds.Length != playerPositions.Length || playerIds.Length != playerNames.Length) {
                throw new ArgumentException("Player IDs and positions must have the same length.");
            }

            lock (_lock) {
                string[] rows = mapString.Split(';');
                int height = rows.Length;
                int width = rows[0].Length;
                Tiles = new TileType[height, width];
                for (int i = 0; i < height; i++) {
                    for (int j = 0; j < width; j++) {
                        Tiles[i, j] = Enum.Parse<TileType>(rows[i][j].ToString());
                    }
                }

                for (int i = 0; i < playerIds.Length; i++) {
                    int playerId = playerIds[i];
                    Position position = playerPositions[i];
                    string name = playerNames[i];
                    PlayerInfos[playerId] = new PlayerInfo(playerId, name, position);
                }

                Duration = duration;

                IsInitialized = true;
                OnMapInitialized?.Invoke();
            }
        }
    }
}