using System;

using Shared;
using Client.Scene;

namespace Client.Handler {

    public class PlayerHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.PlayerMoved:
                    PlayerMoved(message);
                    break;
                case ServerMessageType.PlayerDied:
                    PlayerDied(message);
                    break;
                case ServerMessageType.GameLeft:
                case ServerMessageType.PlayerLeft:
                    PlayerLeft(message);
                    break;
                default:
                    throw new Exception("Cannot handle item message: {type}");
            }
        }

        private void PlayerMoved(NetworkMessage message) {
            int playerId = int.Parse(message.Data["playerId"]);
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);
            Direction direction = Enum.Parse<Direction>(message.Data["d"]);

            map.MovePlayer(playerId, x, y, direction);
        }

        private void PlayerDied(NetworkMessage message) {
            int playerId = int.Parse(message.Data["playerId"]);
            int byPlayerId = int.Parse(message.Data["byPlayerId"]);
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);

            map.KillPlayer(playerId);
            map.TeleportPlayer(playerId, x, y);
            map.IncreaseScore(byPlayerId, 1);
        }

        private void PlayerLeft(NetworkMessage message) {
            int playerId = int.Parse(message.Data["playerId"]);
            map.RemovePlayer(playerId);
        }
    }
}