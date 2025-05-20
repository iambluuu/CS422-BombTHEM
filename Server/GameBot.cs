using Shared;

namespace Server {
    public class GameBot {
        private int _botId;
        private string _roomId;
        private Map _map = null!;
        private Action<int, NetworkMessage>? _sendToSerer;
        private Thread _thread;
        private CancellationTokenSource _cts;
        bool _isGameStarted = false;
        DateTime _startTime;
        (PowerName, PowerName) _powers = (PowerName.None, PowerName.None);

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
            _sendToSerer = sendToServer;

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
                        { "direction", ((Direction)Utils.randomList(movableDirections)).ToString() },
                    }));
                }

                if ((DateTime.Now - _startTime).TotalMilliseconds > 1000 && (Utils.RandomInt(10) == 0 || movableDirections.Count == 0)) {
                    SendToServer(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                        {"x", _map.PlayerInfos[BotId].Position.X.ToString() },
                        {"y", _map.PlayerInfos[BotId].Position.Y.ToString() },
                        {"type", BombType.Normal.ToString() },
                    }));
                }

                if ((DateTime.Now - _startTime).TotalMilliseconds > 1000 && Utils.RandomInt(20) == 0 && (_powers.Item1 != PowerName.None || _powers.Item2 != PowerName.None)) {
                    PowerName power;
                    if (_powers.Item1 != PowerName.None) {
                        power = _powers.Item1;
                    } else {
                        power = _powers.Item2;
                    }

                    SendToServer(NetworkMessage.From(ClientMessageType.UsePowerUp, new() {
                        { "powerUpType", power.ToString() },
                    }));
                }

                Thread.Sleep(200);
            }
        }

        private void SendToServer(NetworkMessage message) {
            _sendToSerer?.Invoke(_botId, message);
        }

        public void HandleResponse(NetworkMessage message) {
            if (_cts.Token.IsCancellationRequested) {
                return;
            }

            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.GameStarted: {
                        SendToServer(NetworkMessage.From(ClientMessageType.GetGameInfo));
                    }
                    break;
                case ServerMessageType.GameInfo: {
                        _map = Map.FromString(message.Data["map"]);
                        int playerCount = int.Parse(message.Data["playerCount"]);
                        int[] playerIds = Array.ConvertAll(message.Data["playerIds"].Split(';'), int.Parse);
                        Position[] playerPositions = Array.ConvertAll(message.Data["playerPositions"].Split(';'), Position.FromString);

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
                        int playerId = int.Parse(message.Data["playerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        Direction direction = Enum.Parse<Direction>(message.Data["d"]);
                        _map.SetPlayerPosition(playerId, x, y);
                    }
                    break;
                case ServerMessageType.PlayerLeft: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        if (_isGameStarted) {
                            _map.PlayerInfos.Remove(playerId);
                        }
                    }
                    break;
                case ServerMessageType.BombPlaced: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        BombType type = Enum.Parse<BombType>(message.Data["type"]);
                        _map.AddBomb(x, y, type);
                    }
                    break;
                case ServerMessageType.BombExploded: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        string[] positions = message.Data["positions"].Split(';');

                        foreach (var pos in positions) {
                            int ex = Position.FromString(pos).X;
                            int ey = Position.FromString(pos).Y;
                            _map.SetTile(ex, ey, TileType.Empty);
                        }

                        _map.RemoveBomb(x, y);
                    }
                    break;
                case ServerMessageType.PlayerDied: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        _map.SetPlayerPosition(playerId, x, y);
                    }
                    break;
                case ServerMessageType.PowerUpPickedUp: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        PowerName powerUpType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        if (playerId == BotId) {
                            if (_powers.Item1 == PowerName.None) {
                                _powers.Item1 = powerUpType;
                            } else if (_powers.Item2 == PowerName.None) {
                                _powers.Item2 = powerUpType;
                            }
                        }
                    }
                    break;
            }
        }
    }
}