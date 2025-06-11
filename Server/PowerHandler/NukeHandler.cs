using Shared;
using Shared.PacketWriter;

namespace Server.PowerHandler {
    public class NukeHandler : PowerUpHandler {
        public override PowerName PowerName => PowerName.Nuke;
        public override Dictionary<byte, object>? Apply(ServerMap map, int playerId, Dictionary<string, object>? parameters = null, int slotNum = -1) {
            if (map.PlayerInfos[playerId].HasPowerUp(PowerName.Nuke)) {
                return null;
            }

            map.PlayerInfos[playerId].ActivePowerUps.Add(new ActivePowerUp(PowerName.Nuke, DateTime.Now, slotNum));

            return new Dictionary<byte, object> {
                { (byte)ServerParams.PlayerId, playerId },
                { (byte)ServerParams.NeedToChange, true },
            };
        }

        public override Dictionary<byte, object>? Use(ServerMap map, int playerId, Dictionary<string, object>? parameters = null) {
            var target = map.PlayerInfos[playerId];
            var activeNuke = target.TryGetActivePowerUp(PowerName.Nuke);
            if (activeNuke == null) {
                return null;
            }

            if (target.UsePowerUp(PowerName.Nuke, activeNuke.SlotNum)) {
                return new Dictionary<byte, object> {
                    { (byte)ServerParams.PlayerId, playerId },
                    { (byte)ServerParams.NeedToChange, true }
                };
            }

            return null;
        }
    }
}