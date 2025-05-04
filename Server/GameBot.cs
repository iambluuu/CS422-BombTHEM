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

        public void Dispose() {
            _cts.Cancel();
            // _thread.Join(); fuck this, fuck deadlock
        }

        private void Run() {
            while (!_cts.Token.IsCancellationRequested) {
                if (_botId == -1 || !_isGameStarted) {
                    Thread.Sleep(300);
                    continue;
                }

                while (!_cts.Token.IsCancellationRequested) {
                    Direction direction = (Direction)Utils.RandomInt(4);
                    if (_map.IsPlayerMovable(_botId, direction)) {
                        SendToServer(NetworkMessage.From(ClientMessageType.MovePlayer, new() {
                            { "direction", direction.ToString() },
                        }));
                        break;
                    }
                }

                Thread.Sleep(300);
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
                            _map.PlayerPositions.Remove(playerId);
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
            }
        }
    }
}