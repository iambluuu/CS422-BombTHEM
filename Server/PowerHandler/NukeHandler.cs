using Shared;

namespace Server.PowerHandler {
    public class NukeHandler : PowerUpHandler {
        public override PowerName PowerName => PowerName.Nuke;
        public override Dictionary<string, object>? Apply(Map map, int playerId, Dictionary<string, object>? parameters = null, int slotNum = -1) {
            if (parameters == null) {
                return null;
            }

            if (map.PlayerInfos[playerId].HasPowerUp(PowerName.Nuke)) {
                return null;
            }

            map.PlayerInfos[playerId].ActivePowerUps.Add(new ActivePowerUp(PowerName.Nuke, DateTime.Now, slotNum));
            return new Dictionary<string, object> {
                { "playerId", playerId },
                { "needToChange", true }
            };
        }

        public override Dictionary<string, object>? Use(Map map, int playerId, Dictionary<string, object>? parameters = null) {
            var target = map.PlayerInfos[playerId];
            var activeNuke = target.TryGetActivePowerUp(PowerName.Nuke);
            if (activeNuke == null) {
                return null;
            }

            if (target.UsePowerUp(PowerName.Nuke, activeNuke.SlotNum)) {
                if (!target.CanUsePowerUp(PowerName.Nuke, activeNuke.SlotNum)) {
                    target.ExpireActivePowerUp(PowerName.Nuke, activeNuke.SlotNum);
                }
                return new Dictionary<string, object> {
                    { "playerId", playerId.ToString() },
                    { "needToChange", true }
                };
            }

            return null;
        }
    }
}