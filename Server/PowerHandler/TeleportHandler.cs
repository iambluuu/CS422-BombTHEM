using Shared;

namespace Server.PowerHandler {
    public class TeleportHandler : PowerUpHandler {
        public override PowerName PowerName => PowerName.Teleport;
        public override Dictionary<string, object>? Apply(Map map, int playerId, Dictionary<string, object>? parameters = null, int slotNum = -1) {
            base.Apply(map, playerId, parameters, slotNum);
            Position currentPos = map.PlayerInfos[playerId].Position;
            // find safe position
            Position newPos = map.GetSafePosition(playerId);
            map.PlayerInfos[playerId].Position = newPos;
            return new Dictionary<string, object> {
                { "oldX", currentPos.X },
                { "oldY", currentPos.Y },
                { "x", newPos.X },
                { "y", newPos.Y },
                { "playerId", playerId },
                { "needToChange", true }
            };
        }
    }
}