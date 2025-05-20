

using System;
using System.Collections.Generic;
using System.Linq;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Shared;
using SharpDX.Direct2D1.Effects;

namespace Client {
    public class PlayerInfo {
        public Position Position;
        public int Score;
        public readonly PlayerSkin SkinId;
        public readonly string Name;

        public PlayerInfo(PlayerSkin skinId, string name, Position position) {
            SkinId = skinId;
            Name = name;
            Position = position;
        }
    }

    public class MapRenderInfo {
        private readonly object _lock = new();
        public int Duration { get; private set; }

        public bool IsInitialized { get; private set; } = false;
        public Action OnMapInitialized = null;

        private TileType[,] Tiles;
        private bool[,] TileLocked;
        public Dictionary<int, PlayerInfo> PlayerInfos { get; private set; } = [];
        // PowerUps: Power-up slots, 0: left, 1: right, (PowerType, quantity)
        public (PowerName, int)[] PowerUps { get; private set; } = [(PowerName.None, 0), (PowerName.None, 0)];
        public List<PowerName> ActivePowerUps { get; private set; } = new(); // Currently active power-ups of THIS CLIENT
        private bool[] powerUpLocked = [false, false];
        public PlayerNode MyNode { private get; set; } = null;
        public int BombCount { get; private set; } = 0;

        private readonly List<(int, int)> BombPosition = new(); // (x, y)
        private readonly List<(int, int, BombType)> NewBomb = new(); // (x, y, bombType)
        private readonly List<(int, int)> NewExplosion = new(); // (x, y)
        private readonly List<(int, int)> DestroyedBlock = new(); // (x, y)
        private readonly List<(int, int)> RemovedBomb = new(); // (x, y)
        private readonly List<(int, int, PowerName)> NewItemDropped = new(); // (x, y, powerUpType)
        private readonly List<(int, int)> RemovedItem = new(); // (x, y)
        private readonly List<(int, PowerName)> ExpiredPlayerPowerUp = new(); // (playerId, powerUpType)
        private readonly List<(int, PowerName)> NewPlayerVFX = new(); // (playerId, powerUpType)
        private readonly List<(int, int, PowerName)> NewEnvVFX = new(); // (x, y, powerUpType)
        private readonly List<int> DeadPlayers = new(); // playerId
        private readonly List<(int, int, int, Direction)> MovedPlayers = new(); // playerId, x, y, direction
        private readonly List<(int, int, int)> TeleportedPlayers = new(); // playerId, x, y
        private readonly List<int> RemovedPlayers = new(); // playerId

        public List<(int, int, BombType)> FlushNewBomb() => FlushList(NewBomb);
        public List<(int, int)> FlushNewExplosion() => FlushList(NewExplosion);
        public List<(int, int)> FlushDestroyedBlock() => FlushList(DestroyedBlock);
        public List<(int, int)> FlushRemovedBomb() => FlushList(RemovedBomb);
        public List<(int, int, PowerName)> FlushNewItemDropped() => FlushList(NewItemDropped);
        public List<(int, int)> FlushRemovedItem() => FlushList(RemovedItem);
        public List<(int, PowerName)> FlushExpiredPlayerPowerUp() => FlushList(ExpiredPlayerPowerUp);
        public List<(int, PowerName)> FlushNewPlayerVFX() => FlushList(NewPlayerVFX);
        public List<(int, int, PowerName)> FlushNewEnvVFX() => FlushList(NewEnvVFX);
        public List<int> FlushDeadPlayers() => FlushList(DeadPlayers);
        public List<(int, int, int, Direction)> FlushMovedPlayers() => FlushList(MovedPlayers);
        public List<(int, int, int)> FlushTeleportedPlayers() => FlushList(TeleportedPlayers);
        public List<int> FlushRemovedPlayers() => FlushList(RemovedPlayers);

        internal List<T> FlushList<T>(List<T> list) {
            lock (_lock) {
                var flushed = new List<T>(list);
                list.Clear();
                return flushed;
            }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool HasActivePowerUp(PowerName powerType) {
            lock (_lock) {
                return ActivePowerUps.Contains(powerType);
            }
        }

        public Position GetMyPosition() {
            return PlayerInfos[NetworkManager.Instance.ClientId].Position;
        }

        public Vector2 GetMySpritePosition() {
            return MyNode.Position;
        }

        public void MovePlayer(int playerId, Direction direction) {
            lock (_lock) {
                if (!PlayerInfos.ContainsKey(playerId)) {
                    throw new KeyNotFoundException($"Map.MovePlayer: Player ID {playerId} not found");
                }

                Position newPosition = PlayerInfos[playerId].Position.Move(direction);
                if (!IsInBounds(newPosition.X, newPosition.Y)) {
                    return;
                }

                if (GetTile(newPosition.X, newPosition.Y) != TileType.Empty) {
                    return;
                }

                if (PlayerInfos.ContainsKey(playerId)) {
                    PlayerInfos[playerId].Position = newPosition;
                    MovedPlayers.Add((playerId, newPosition.X, newPosition.Y, direction));
                }
            }
        }

        public bool IsPlayerMovable(int playerId, Direction direction) {
            lock (_lock) {
                if (!PlayerInfos.ContainsKey(playerId)) {
                    throw new KeyNotFoundException($"Map.IsPlayerMovable: Player ID {playerId} not found");
                }

                Position newPosition = PlayerInfos[playerId].Position.Move(direction);
                return IsInBounds(newPosition.X, newPosition.Y) && GetTile(newPosition.X, newPosition.Y) == TileType.Empty;
            }
        }

        public Position GetNearestCell() {
            var currentPos = GetMySpritePosition();
            var currentIdx = GetMyPosition();
            Position nearestCell = null;
            float minDistance = float.MaxValue;

            for (int i = currentIdx.X - 2; i <= currentIdx.X + 2; i++) {
                for (int j = currentIdx.Y - 2; j <= currentIdx.Y + 2; j++) {
                    if (IsInBounds(i, j) && GetTile(i, j) == TileType.Empty && !HasBombAt(i, j)) {
                        float distance = Math.Abs(currentPos.Y - (i * GameValues.TILE_SIZE)) + Math.Abs(currentPos.X - (j * GameValues.TILE_SIZE));
                        if (distance < GameValues.TILE_SIZE / 3 && distance < minDistance) {
                            minDistance = distance;
                            nearestCell = new Position(i, j);
                        }
                    }
                }
            }

            return nearestCell;
        }

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

        public void RemovePlayer(int playerId) {
            lock (_lock) {
                PlayerInfos.Remove(playerId);
            }
        }

        public void KillPlayer(int playerId) {
            lock (_lock) {
                DeadPlayers.Add(playerId);
            }
        }

        public void MovePlayer(int playerId, int x, int y, Direction direction) {
            lock (_lock) {
                if (PlayerInfos[playerId].Position.X == x && PlayerInfos[playerId].Position.Y == y) {
                    return;
                }

                PlayerInfos[playerId].Position = new Position(x, y);
                MovedPlayers.Add((playerId, x, y, direction));
            }
        }

        public void TeleportPlayer(int playerId, int x, int y) {
            lock (_lock) {
                PlayerInfos[playerId].Position = new Position(x, y);
                TeleportedPlayers.Add((playerId, x, y));
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

        public bool HasBombAt(int x, int y) {
            return BombPosition.Contains((x, y));
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
                    if (Tiles[ex, ey] == TileType.Grass) {
                        Tiles[ex, ey] = TileType.Empty;
                        DestroyedBlock.Add((ex, ey));
                    }
                }

                if (byPlayerId == NetworkManager.Instance.ClientId && isCounted) {
                    BombCount--;
                }
            }
        }

        public void PowerUpExpired(int playerId, PowerName powerUpType) {
            lock (_lock) {
                if (playerId == NetworkManager.Instance.ClientId) {
                    ActivePowerUps.Remove(powerUpType);
                }
                ExpiredPlayerPowerUp.Add((playerId, powerUpType));
            }
        }

        public void PowerUpUsed(int slotNum) {
            lock (_lock) {
                powerUpLocked[slotNum] = false;
                PowerUps[slotNum].Item2--;
                if (PowerUps[slotNum].Item2 == 0) {
                    PowerUps[slotNum] = (PowerName.None, 0);
                }
            }
        }

        public void AddActivePowerUp(PowerName powerType) {
            lock (_lock) {
                ActivePowerUps.Add(powerType);
            }
        }

        public void AddEnvVFX(int x, int y, PowerName powerType) {
            lock (_lock) {
                NewEnvVFX.Add((x, y, powerType));
            }
        }

        public void AddPlayerVFX(int playerId, PowerName powerType) {
            lock (_lock) {
                NewPlayerVFX.Add((playerId, powerType));
            }
        }

        public void ItemExpired(int x, int y) {
            lock (_lock) {
                RemovedItem.Add((x, y));
            }
        }

        public void ItemPickedUp(int playerId, int x, int y, PowerName powerType) {
            lock (_lock) {
                RemovedItem.Add((x, y));
                if (playerId == NetworkManager.Instance.ClientId) {
                    for (int i = 0; i < PowerUps.Length; i++) {
                        if (PowerUps[i].Item1 == PowerName.None) {
                            PowerUps[i] = (powerType, GameplayConfig.PowerUpQuantity[powerType]);
                            break;
                        }
                    }
                }
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

        public bool IsSlotLocked(int slotNum) {
            lock (_lock) {
                return powerUpLocked[slotNum];
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

        public bool LockTile(int x, int y) {
            lock (_lock) {
                if (TileLocked[x, y]) {
                    return false;
                }
                TileLocked[x, y] = true;
                return true;
            }
        }

        public void UnlockTile(int x, int y) {
            lock (_lock) {
                TileLocked[x, y] = false;
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
                Width = width;
                Height = height;
                Tiles = new TileType[height, width];
                TileLocked = new bool[height, width];
                PlayerInfos = new Dictionary<int, PlayerInfo>(playerIds.Length);
                Reset();

                for (int i = 0; i < height; i++) {
                    for (int j = 0; j < width; j++) {
                        TileLocked[i, j] = false;
                        Tiles[i, j] = Enum.Parse<TileType>(rows[i][j].ToString());
                    }
                }

                for (int i = 0; i < playerIds.Length; i++) {
                    int playerId = playerIds[i];
                    Position position = playerPositions[i];
                    string name = playerNames[i];
                    PlayerInfos[playerId] = new PlayerInfo((PlayerSkin)i, name, position);
                }

                Duration = duration;
                IsInitialized = true;
            }
            OnMapInitialized?.Invoke();
        }

        private void Reset() {
            IsInitialized = false;
            PowerUps = [(PowerName.None, 0), (PowerName.None, 0)];
            ActivePowerUps = [];
            BombCount = 0;

            BombPosition.Clear();
            NewBomb.Clear();
            NewExplosion.Clear();
            DestroyedBlock.Clear();
            RemovedBomb.Clear();
            NewItemDropped.Clear();
            RemovedItem.Clear();
            ExpiredPlayerPowerUp.Clear();
            NewPlayerVFX.Clear();
            NewEnvVFX.Clear();
            DeadPlayers.Clear();
            MovedPlayers.Clear();
            RemovedPlayers.Clear();
            PlayerInfos.Clear();
        }

        public Dictionary<int, PlayerSkin> GetSkinMapping() {
            lock (_lock) {
                foreach (var kvp in PlayerInfos) {
                    Console.WriteLine($"Player ID: {kvp.Key}, Skin ID: {kvp.Value.SkinId}");
                }
                return PlayerInfos.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.SkinId);
            }
        }
    }
}