using System;

using Shared;
using Client.Scene;
using Client.Audio;
using Shared.PacketWriter;
using Client.Network;

namespace Client.Handler {

    public class PlayerHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            switch ((ServerMessageType)message.Type.Name) {
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
                    throw new Exception($"Cannot handle item message: {message.Type.Name}");
            }
        }

        private void PlayerMoved(NetworkMessage message) {
            int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
            int x = message.Data[(byte)ServerParams.X] as ushort? ?? -1;
            int y = message.Data[(byte)ServerParams.Y] as ushort? ?? -1;
            Direction direction = message.Data[(byte)ServerParams.Direction] is byte b ? (Direction)b : Direction.None;

            // if (playerId != NetworkManager.Instance.ClientId) {
            //     Console.WriteLine($"[PlayerHandler] Player {playerId} moved to ({x}, {y}) in direction {direction}");
            // }

            if (playerId == -1 || x < 0 || y < 0 || direction == Direction.None) {
                Console.WriteLine($"Invalid player move data: {message.Data}");
                return;
            }

            map.MovePlayer(playerId, x, y, direction);
        }

        private void PlayerDied(NetworkMessage message) {
            int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
            int byPlayerId = message.Data[(byte)ServerParams.ByPlayerId] as int? ?? -1;
            int x = message.Data[(byte)ServerParams.X] as ushort? ?? -1;
            int y = message.Data[(byte)ServerParams.Y] as ushort? ?? -1;

            map.KillPlayer(playerId);
            map.TeleportPlayer(playerId, x, y);
            map.IncreaseScore(byPlayerId, 1);
            SoundPlayer.Play("Hit");
        }

        private void PlayerLeft(NetworkMessage message) {
            int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
            map.RemovePlayer(playerId);
        }
    }
}