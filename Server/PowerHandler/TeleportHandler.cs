using Shared;
using Shared.PacketWriter;

namespace Server.PowerHandler {
    public class TeleportHandler : PowerUpHandler {
        public override PowerName PowerName => PowerName.Teleport;
        public override Dictionary<byte, object>? Apply(Map map, int playerId, Dictionary<string, object>? parameters = null, int slotNum = -1) {
            base.Apply(map, playerId, parameters, slotNum);
            Position currentPos = map.PlayerInfos[playerId].Position;
            // find safe position
            Position newPos = map.GetSafePosition(playerId);
            map.PlayerInfos[playerId].Position = newPos;
            return new Dictionary<byte, object> {
                { (byte)ServerParams.OldX, currentPos.X },
                { (byte)ServerParams.OldY, currentPos.Y },
                { (byte)ServerParams.X, newPos.X },
                { (byte)ServerParams.Y, newPos.Y },
                { (byte)ServerParams.PlayerId, playerId },
                { (byte)ServerParams.NeedToChange, true }
            };
        }
    }
}