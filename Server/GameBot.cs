using System.Text.Json;
using Shared;
using Shared.PacketWriter;

namespace Server {
    public class GameBot {
        private int _botId;
        private string _roomId;
        private ServerMap _map = null!;
        private Action<int, NetworkMessage>? _sendToServer;
        private Thread _thread;
        private CancellationTokenSource _cts;
        bool _isGameStarted = false;
        private (PowerName, int, bool)[] _powerUps = {
            (PowerName.None, 0, false),
            (PowerName.None, 0, false),
        }; // Name, Quantity, Activated
        DateTime _startTime;

        // Heuristic state tracking
        private Dictionary<Position, DateTime> _dangerZones = new();
        private Position? _lastPosition;
        private int _stuckCounter = 0;
        private DateTime _lastBombTime = DateTime.MinValue;
        private DateTime _lastMoveTime = DateTime.MinValue;

        public int BotId {
            get {
                return _botId;
            }
        }
        public string RoomId {
            get {
                return _roomId;
            }
        }

        public GameBot(int botId, string roomId, Action<int, NetworkMessage> sendToServer) {
            _botId = botId;
            _roomId = roomId;
            _sendToServer = sendToServer;

            _cts = new CancellationTokenSource();
            _thread = new Thread(Run) {
                IsBackground = true,
            };
            _thread.Start();
        }

        public void Stop() {
            _isGameStarted = false;
            Console.WriteLine($"Bot {_botId} stopped");
        }

        public void Dispose() {
            _cts.Cancel();
        }

        private void Run() {
            while (!_cts.Token.IsCancellationRequested) {
                if (_botId == -1 || !_isGameStarted) {
                    Thread.Sleep(200);
                    continue;
                }

                try {
                    UpdateDangerZones();

                    // Priority system: Dodge > Use PowerUp > Move/Attack
                    if (ShouldDodge()) {
                        DodgeBombs();
                    } else if (ShouldUsePowerUp()) {
                        UseBestPowerUp();
                    } else {
                        MakeStrategicMove();
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Bot {_botId} error: {ex.Message}");
                }

                Thread.Sleep(150); // Slightly faster reaction time
            }
        }

        private void UpdateDangerZones() {
            _dangerZones.Clear();

            foreach (var bomb in _map.Bombs) {
                var explodeTime = bomb.PlaceTime.AddSeconds(3); // Assume 3 second fuse

                // Mark bomb position and explosion range as dangerous
                List<Position> dangerPositions = new() { bomb.Position };

                // Calculate explosion pattern based on bomb type
                if (bomb.Type == BombType.Normal) {
                    dangerPositions.AddRange(GetNormalBombRange(bomb.Position));
                } else if (bomb.Type == BombType.Nuke) {
                    dangerPositions.AddRange(GetNukeBombRange(bomb.Position));
                }

                foreach (var pos in dangerPositions) {
                    _dangerZones[pos] = explodeTime;
                }
            }
        }

        private List<Position> GetNormalBombRange(Position bombPos) {
            List<Position> range = new();
            List<Position> directions = new() {
                new(0, -1), new(0, 1), new(-1, 0), new(1, 0)
            };

            foreach (var dir in directions) {
                for (int i = 1; i <= 2; i++) {
                    int newX = bombPos.X + dir.X * i;
                    int newY = bombPos.Y + dir.Y * i;

                    if (_map.IsInBounds(newX, newY)) {
                        if (_map.GetTile(newX, newY) == TileType.Wall) break;
                        range.Add(new Position(newX, newY));
                        if (_map.GetTile(newX, newY) == TileType.Grass) break;
                    }
                }
            }
            return range;
        }

        private List<Position> GetNukeBombRange(Position bombPos) {
            List<Position> range = new();
            List<Position> directions = new() {
                new(0, -1), new(0, 1), new(-1, 0), new(1, 0)
            };

            foreach (var dir in directions) {
                for (int i = 1; i <= Math.Max(_map.Width, _map.Height); i++) {
                    int newX = bombPos.X + dir.X * i;
                    int newY = bombPos.Y + dir.Y * i;

                    if (_map.IsInBounds(newX, newY)) {
                        var tile = _map.GetTile(newX, newY);
                        if (tile == TileType.Empty || tile == TileType.Grass) {
                            range.Add(new Position(newX, newY));
                        } else {
                            break;
                        }
                    }
                }
            }
            return range;
        }

        private bool ShouldDodge() {
            var myPos = _map.PlayerInfos[BotId].Position;

            // Check if current position is dangerous
            if (_dangerZones.ContainsKey(myPos)) {
                var explodeTime = _dangerZones[myPos];
                return (explodeTime - DateTime.Now).TotalSeconds < 2.5; // Dodge with 2.5s buffer
            }

            return false;
        }

        private void DodgeBombs() {
            var myPos = _map.PlayerInfos[BotId].Position;
            var safeMoves = GetSafeMoves();

            if (safeMoves.Count > 0) {
                // Prefer moves that get us furthest from danger
                var bestMove = safeMoves.OrderByDescending(dir => {
                    var newPos = myPos.Move(dir);
                    return GetSafetyScore(newPos);
                }).First();

                SendToServer(NetworkMessage.From(ClientMessageType.MovePlayer, new() {
                    { (byte)ClientParams.Direction, (byte)bestMove},
                }));

                _lastMoveTime = DateTime.Now;
            } else if (HasPowerUp(PowerName.Teleport)) {
                // Emergency teleport if no safe moves
                UsePowerUp(PowerName.Teleport);
            }
        }

        private List<Direction> GetSafeMoves() {
            List<Direction> safeMoves = new();
            var myPos = _map.PlayerInfos[BotId].Position;

            for (int i = 0; i < 4; i++) {
                var direction = (Direction)i;
                if (_map.IsPlayerMovable(BotId, direction)) {
                    var newPos = myPos.Move(direction);

                    // Check if new position is safe from bombs
                    bool isSafe = true;
                    if (_dangerZones.ContainsKey(newPos)) {
                        var explodeTime = _dangerZones[newPos];
                        if ((explodeTime - DateTime.Now).TotalSeconds < 1.5) {
                            isSafe = false;
                        }
                    }

                    if (isSafe) {
                        safeMoves.Add(direction);
                    }
                }
            }

            return safeMoves;
        }

        private double GetSafetyScore(Position pos) {
            double score = 0;

            // Distance from bombs
            foreach (var bomb in _map.Bombs) {
                double distance = Math.Sqrt(Math.Pow(pos.X - bomb.Position.X, 2) + Math.Pow(pos.Y - bomb.Position.Y, 2));
                score += distance;
            }

            // Distance from other players (avoid crowding)
            foreach (var player in _map.PlayerInfos.Values) {
                if (player.Position.X != pos.X || player.Position.Y != pos.Y) {
                    double distance = Math.Sqrt(Math.Pow(pos.X - player.Position.X, 2) + Math.Pow(pos.Y - player.Position.Y, 2));
                    score += distance * 0.5;
                }
            }

            return score;
        }

        private void MakeStrategicMove() {
            if ((DateTime.Now - _lastMoveTime).TotalMilliseconds < 200) {
                return; // Wait for animation to finish, avoid spamming moves
            }

            var myPos = _map.PlayerInfos[BotId].Position;

            // Check if we're stuck
            if (_lastPosition != null && _lastPosition.Equals(myPos)) {
                _stuckCounter++;
            } else {
                _stuckCounter = 0;
            }
            _lastPosition = new Position(myPos.X, myPos.Y);

            // Strategic bomb placement
            if (ShouldPlaceBomb()) {
                PlaceStrategicBomb();
                return;
            }

            // Movement strategy
            var bestMove = GetBestMove();
            if (bestMove != Direction.None) {
                SendToServer(NetworkMessage.From(ClientMessageType.MovePlayer, new() {
                    { (byte)ClientParams.Direction, (byte)bestMove},
                }));
                _lastMoveTime = DateTime.Now;
            }
        }

        private bool ShouldPlaceBomb() {
            if ((DateTime.Now - _lastBombTime).TotalMilliseconds < 1000) return false;
            if ((DateTime.Now - _startTime).TotalMilliseconds < 1000) return false;

            var myPos = _map.PlayerInfos[BotId].Position;

            // Don't place bomb if we're in danger
            if (_dangerZones.ContainsKey(myPos)) return false;

            // Place bomb if there are destructible blocks nearby
            if (HasDestructibleBlocksNearby(myPos)) return true;

            // Place bomb if enemy is nearby and we have escape route
            if (HasEnemyNearby(myPos) && HasEscapeRoute(myPos)) return true;

            // Random placement occasionally to create chaos
            return Utils.RandomInt(15) == 0;
        }

        private void PlaceStrategicBomb() {
            var myPos = _map.PlayerInfos[BotId].Position;
            BombType bombType = BombType.Normal;

            // Use nuke if we have it and there are many targets
            if (HasPowerUp(PowerName.Nuke) && CountNearbyTargets(myPos) >= 3) {
                bombType = BombType.Nuke;
            }

            SendToServer(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                { (byte)ClientParams.X, (ushort)myPos.X },
                { (byte)ClientParams.Y, (ushort)myPos.Y },
                { (byte)ClientParams.BombType, bombType },
            }));

            _lastBombTime = DateTime.Now;
        }

        private Direction GetBestMove() {
            var myPos = _map.PlayerInfos[BotId].Position;
            var safeMoves = GetSafeMoves();

            if (safeMoves.Count == 0) return Direction.None;

            // If stuck, try random safe direction
            if (_stuckCounter > 3) {
                return Utils.randomList(safeMoves);
            }

            // Score each move
            var scoredMoves = safeMoves.Select(dir => new {
                Direction = dir,
                Score = ScoreMove(myPos, dir)
            }).OrderByDescending(x => x.Score).ToList();

            return scoredMoves.First().Direction;
        }

        private double ScoreMove(Position currentPos, Direction direction) {
            var newPos = currentPos.Move(direction);
            double score = 0;

            // Safety score
            score += GetSafetyScore(newPos);

            // Item collection
            if (_map.HasItem(newPos.X, newPos.Y)) {
                score += 50;
            }

            // Prefer positions that can lead to strategic bomb placement
            score += CountNearbyTargets(newPos) * 10;

            // Avoid edges unless necessary
            if (newPos.X == 0 || newPos.X == _map.Height - 1 || newPos.Y == 0 || newPos.Y == _map.Width - 1) {
                score -= 5;
            }

            // Exploration bonus (prefer unvisited areas)
            score += GetExplorationBonus(newPos);

            return score;
        }

        private bool HasDestructibleBlocksNearby(Position pos) {
            List<Position> directions = new() {
                new(0, -1), new(0, 1), new(-1, 0), new(1, 0)
            };

            foreach (var dir in directions) {
                for (int i = 1; i <= 2; i++) {
                    int newX = pos.X + dir.X * i;
                    int newY = pos.Y + dir.Y * i;

                    if (_map.IsInBounds(newX, newY)) {
                        if (_map.GetTile(newX, newY) == TileType.Grass) return true;
                        if (_map.GetTile(newX, newY) == TileType.Wall) break;
                    }
                }
            }
            return false;
        }

        private bool HasEnemyNearby(Position pos) {
            foreach (var player in _map.PlayerInfos) {
                if (player.Key != BotId) {
                    double distance = Math.Sqrt(Math.Pow(pos.X - player.Value.Position.X, 2) +
                                              Math.Pow(pos.Y - player.Value.Position.Y, 2));
                    if (distance <= 3) return true;
                }
            }
            return false;
        }

        private bool HasEscapeRoute(Position pos) {
            var safeMoves = GetSafeMoves();
            return safeMoves.Count > 0;
        }

        private int CountNearbyTargets(Position pos) {
            int count = 0;
            List<Position> directions = new() {
                new(0, -1), new(0, 1), new(-1, 0), new(1, 0)
            };

            foreach (var dir in directions) {
                for (int i = 1; i <= 2; i++) {
                    int newX = pos.X + dir.X * i;
                    int newY = pos.Y + dir.Y * i;

                    if (_map.IsInBounds(newX, newY)) {
                        var tile = _map.GetTile(newX, newY);
                        if (tile == TileType.Grass) count++;
                        if (tile == TileType.Wall) break;

                        // Count nearby enemies
                        foreach (var player in _map.PlayerInfos) {
                            if (player.Key != BotId &&
                                player.Value.Position.X == newX &&
                                player.Value.Position.Y == newY) {
                                count += 2; // Enemies are more valuable targets
                            }
                        }
                    }
                }
            }
            return count;
        }

        private double GetExplorationBonus(Position pos) {
            // Simple exploration heuristic - prefer positions further from center
            double centerX = _map.Height / 2.0;
            double centerY = _map.Width / 2.0;
            return Math.Sqrt(Math.Pow(pos.X - centerX, 2) + Math.Pow(pos.Y - centerY, 2)) * 0.1;
        }

        private bool ShouldUsePowerUp() {
            // Use shield if in immediate danger
            if (HasPowerUp(PowerName.Shield) && ShouldDodge()) {
                return true;
            }

            // Use teleport if trapped
            if (HasPowerUp(PowerName.Teleport) && GetSafeMoves().Count == 0) {
                return true;
            }

            // Use MoreBombs when engaging enemies
            if ((HasPowerUp(PowerName.MoreBombs) || HasPowerUp(PowerName.Nuke)) && HasEnemyNearby(_map.PlayerInfos[BotId].Position)) {
                return true;
            }

            return false;
        }

        private void UseBestPowerUp() {
            var myPos = _map.PlayerInfos[BotId].Position;

            // Priority order: Shield > Teleport > MoreBombs > Nuke
            if (HasPowerUp(PowerName.Shield) && ShouldDodge()) {
                UsePowerUp(PowerName.Shield);
            } else if (HasPowerUp(PowerName.Teleport) && GetSafeMoves().Count == 0) {
                UsePowerUp(PowerName.Teleport);
            } else if (HasEnemyNearby(myPos)) {
                if (HasPowerUp(PowerName.Nuke)) {
                    UsePowerUp(PowerName.Nuke);
                } else if (HasPowerUp(PowerName.MoreBombs)) {
                    UsePowerUp(PowerName.MoreBombs);
                }
            }
        }

        private bool HasPowerUp(PowerName powerName) {
            for (int i = 0; i < _powerUps.Length; i++) {
                if (_powerUps[i].Item1 == powerName && _powerUps[i].Item2 > 0) {
                    return true;
                }
            }
            return false;
        }

        private void UsePowerUp(PowerName powerName) {
            for (int i = 0; i < _powerUps.Length; i++) {
                if (_powerUps[i].Item1 == powerName && _powerUps[i].Item2 > 0) {
                    if (powerName == PowerName.Nuke && _powerUps[i].Item3) {
                        // Console.WriteLine($"[Bot]Nuke power-up already used from slot {i}");
                        return;
                    }

                    // Console.WriteLine($"[Bot]Using power-up: {powerName} from slot {i}");
                    SendToServer(NetworkMessage.From(ClientMessageType.UsePowerUp, new() {
                        { (byte)ClientParams.SlotNum, (byte)i },
                        { (byte)ClientParams.PowerUpType, (byte)powerName },
                    }));
                    break;
                }
            }
        }

        private void SendToServer(NetworkMessage message) {
            _sendToServer?.Invoke(_botId, message);
        }

        public void HandleResponse(NetworkMessage message) {
            if (_cts.Token.IsCancellationRequested) {
                return;
            }

            switch ((ServerMessageType)message.Type.Name) {
                case ServerMessageType.GameStarted: {
                        SendToServer(NetworkMessage.From(ClientMessageType.GetGameInfo));
                    }
                    break;
                case ServerMessageType.GameInfo: {
                        _map = ServerMap.FromString(message.Data[(byte)ServerParams.Map] as string ?? string.Empty);
                        int playerCount = message.Data[(byte)ServerParams.PlayerCount] as int? ?? 0;
                        int[] playerIds = message.Data[(byte)ServerParams.PlayerIds] as int[] ?? Array.Empty<int>();
                        Position[] playerPositions = message.Data[(byte)ServerParams.Positions] as Position[] ?? Array.Empty<Position>();

                        for (int i = 0; i < playerCount; i++) {
                            int playerId = playerIds[i];
                            int x = playerPositions[i].X;
                            int y = playerPositions[i].Y;
                            _map.SetPlayerPosition(playerId, x, y);
                        }

                        _isGameStarted = true;
                        _startTime = DateTime.Now;
                    }
                    break;
                case ServerMessageType.PlayerMoved: {
                        int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
                        int x = message.Data[(byte)ServerParams.X] as int? ?? -1;
                        int y = message.Data[(byte)ServerParams.Y] as int? ?? -1;

                        if (playerId == -1 || x < 0 || y < 0) {
                            Console.WriteLine($"[Bot]Invalid player move data: {x} {y} {JsonSerializer.Serialize(message.Data)}");
                            break;
                        }

                        _map.SetPlayerPosition(playerId, x, y);
                    }
                    break;
                case ServerMessageType.PlayerLeft: {
                        int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
                        if (playerId == -1) {
                            Console.WriteLine($"[Bot]Invalid player left data: {playerId} {JsonSerializer.Serialize(message.Data)}");
                            break;
                        }

                        if (_isGameStarted) {
                            _map.PlayerInfos.Remove(playerId);
                        }
                    }
                    break;
                case ServerMessageType.BombPlaced: {
                        if (message.Data.TryGetValue((byte)ServerParams.Invalid, out var invalid) && invalid is bool isInvalid && isInvalid) {
                            break;
                        }
                        int x = message.Data[(byte)ServerParams.X] as int? ?? -1;
                        int y = message.Data[(byte)ServerParams.Y] as int? ?? -1;
                        BombType type = message.Data.TryGetValue((byte)ServerParams.BombType, out var bombType) ? (BombType)bombType : BombType.Normal;
                        if (x < 0 || y < 0) {
                            Console.WriteLine($"[Bot]Invalid bomb placement data: {x}, {y}, {JsonSerializer.Serialize(message.Data)}");
                            break;
                        }

                        _map.AddBomb(x, y, type);
                    }
                    break;
                case ServerMessageType.BombExploded: {
                        if (message.Data.TryGetValue((byte)ServerParams.Invalid, out var invalid) && invalid is bool isInvalid && isInvalid) {
                            break;
                        }
                        int x = message.Data[(byte)ServerParams.X] as int? ?? -1;
                        int y = message.Data[(byte)ServerParams.Y] as int? ?? -1;
                        if (x < 0 || y < 0) {
                            Console.WriteLine($"[Bot]Invalid bomb explosion data: {x}, {y}, {JsonSerializer.Serialize(message.Data)}");
                            break;
                        }
                        Position[] positions = message.Data[(byte)ServerParams.Positions] as Position[] ?? Array.Empty<Position>();

                        foreach (var pos in positions) {
                            int ex = pos.X;
                            int ey = pos.Y;
                            _map.SetTile(ex, ey, TileType.Empty);
                        }

                        _map.RemoveBomb(x, y);
                    }
                    break;
                case ServerMessageType.PlayerDied: {
                        int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
                        int x = message.Data[(byte)ServerParams.X] as int? ?? -1;
                        int y = message.Data[(byte)ServerParams.Y] as int? ?? -1;

                        if (x < 0 || y < 0 || playerId < 0) {
                            Console.WriteLine($"[Bot]Invalid player death data: {message.Data}");
                            break;
                        }

                        _map.SetPlayerPosition(playerId, x, y);
                    }
                    break;
                case ServerMessageType.ItemPickedUp: {
                        int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
                        PowerName powerType = message.Data[(byte)ServerParams.PowerUpType] is PowerName b ? b : PowerName.None;

                        if (playerId == _botId) {
                            // Console.WriteLine($"[Bot]Power-up picked up: {powerType} by player {playerId}");
                            for (int i = 0; i < _powerUps.Length; i++) {
                                if (_powerUps[i].Item1 == PowerName.None) {
                                    _powerUps[i] = (powerType, GameplayConfig.PowerUpQuantity[powerType], false);
                                    break;
                                }
                            }

                            // Console.WriteLine($"[Bot]Current power-ups: {string.Join(", ", _powerUps.Select(p => $"{p.Item1}({p.Item2})"))}");
                        }
                    }
                    break;
                case ServerMessageType.PowerUpUsed: {
                        if (message.Data.TryGetValue((byte)ServerParams.Invalid, out var invalid) && invalid is bool isInvalid && isInvalid) {
                            break;
                        }

                        int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
                        if (playerId != _botId) {
                            break; // Only handle power-ups for this bot
                        }

                        int slotNum = message.Data[(byte)ServerParams.SlotNum] as int? ?? -1;

                        if (slotNum < 0 || slotNum >= _powerUps.Length || _powerUps[slotNum].Item1 == PowerName.None || _powerUps[slotNum].Item2 <= 0) {
                            Console.WriteLine($"[Bot]Invalid power-up slot number: {slotNum}, {_powerUps[slotNum]}");
                            break;
                        }
                        PowerName powerUpType = message.Data[(byte)ServerParams.PowerUpType] is byte b ? (PowerName)b : PowerName.None;
                        if (powerUpType == PowerName.Nuke && _powerUps[slotNum].Item3 == false) {
                            _powerUps[slotNum] = (PowerName.Nuke, _powerUps[slotNum].Item2, true);
                            break;
                        }

                        _powerUps[slotNum] = (_powerUps[slotNum].Item1, _powerUps[slotNum].Item2 - 1, true);
                        if (_powerUps[slotNum].Item2 <= 0) {
                            _powerUps[slotNum] = (PowerName.None, 0, false);
                        }
                    }
                    break;
            }
        }
    }
}