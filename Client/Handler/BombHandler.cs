using System;

using Shared;
using Client.Scene;
using Client.Audio;
using Shared.PacketWriter;

namespace Client.Handler {

    public class BombHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            switch ((ServerMessageType)message.Type.Name) {
                case ServerMessageType.BombPlaced:
                    BombPlaced(message);
                    break;
                case ServerMessageType.BombExploded:
                    BombExploded(message);
                    break;
                default:
                    throw new Exception($"Cannot handle item message: {message.Type.Name}");
            }
        }

        private void BombPlaced(NetworkMessage message) {
            if (message.Data.TryGetValue((byte)ServerParams.Invalid, out object invalidObj)) {
                if (invalidObj is bool isInvalid && isInvalid) {
                    return;
                }
            }
            int x = message.Data[(byte)ServerParams.X] as ushort? ?? -1;
            int y = message.Data[(byte)ServerParams.Y] as ushort? ?? -1;
            int byPlayerId = message.Data[(byte)ServerParams.ByPlayerId] as int? ?? -1;
            map.UnlockTile(x, y);

            // BombType type = Enum.Parse<BombType>(message.Data["type"]);
            // bool isCounted = bool.Parse(message.Data["isCounted"]);
            BombType type = message.Data[(byte)ServerParams.BombType] is byte b ? (BombType)b : BombType.Normal;
            bool isCounted = message.Data[(byte)ServerParams.IsCounted] as bool? ?? true;

            map.BombPlaced(x, y, type, byPlayerId, isCounted);
            SoundPlayer.Play("Whoosh", 1.5f);
        }

        private void BombExploded(NetworkMessage message) {
            if (message.Data.TryGetValue((byte)ServerParams.Invalid, out object invalidObj)) {
                if (invalidObj is bool isInvalid && isInvalid) {
                    return;
                }
            }

            int x = message.Data[(byte)ServerParams.X] as ushort? ?? -1;
            int y = message.Data[(byte)ServerParams.Y] as ushort? ?? -1;
            Position[] positions = message.Data[(byte)ServerParams.Positions] as Position[] ?? Array.Empty<Position>();
            int byPlayerId = message.Data[(byte)ServerParams.ByPlayerId] as int? ?? -1;
            bool isCounted = message.Data[(byte)ServerParams.IsCounted] as bool? ?? true;

            if (x == -1 || y == -1 || byPlayerId == -1) {
                throw new Exception("Invalid bomb explosion data received.");
            }

            map.BombExploded(x, y, positions, byPlayerId, isCounted);
            SoundPlayer.Play("Explosion");
        }
    }
}