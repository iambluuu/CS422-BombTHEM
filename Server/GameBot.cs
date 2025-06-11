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
        DateTime _startTime;

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

                List<int> movableDirections = [];
                for (int i = 0; i < 4; i++) {
                    if (_map.IsPlayerMovable(BotId, (Direction)i)) {
                        movableDirections.Add(i);
                    }
                }

                if (movableDirections.Count != 0) {
                    SendToServer(NetworkMessage.From(ClientMessageType.MovePlayer, new() {
                        { (byte)ClientParams.Direction, (byte)Utils.randomList(movableDirections)},
                    }));
                }

                if ((DateTime.Now - _startTime).TotalMilliseconds > 1000 && (Utils.RandomInt(10) == 0 || movableDirections.Count == 0)) {
                    SendToServer(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                        { (byte)ClientParams.X, (ushort)_map.PlayerInfos[BotId].Position.X },
                        { (byte)ClientParams.Y, (ushort)_map.PlayerInfos[BotId].Position.Y },
                        { (byte)ClientParams.BombType, BombType.Normal },
                    }));
                }

                if ((DateTime.Now - _startTime).TotalMilliseconds > 1000 && Utils.RandomInt(20) == 0) {
                    // Try to use power up here
                }

                Thread.Sleep(200);
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
                        // Handle item pickup
                    }
                    break;
                case ServerMessageType.PowerUpUsed: {
                        if (message.Data.TryGetValue((byte)ServerParams.Invalid, out var invalid) && invalid is bool isInvalid && isInvalid) {
                            break;
                        }

                        // Handle power-up usage
                    }
                    break;
            }
        }
    }
}