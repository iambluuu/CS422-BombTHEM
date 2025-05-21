using Shared;

namespace Server.PowerHandler {
    public class MoreBombsHandler : PowerUpHandler {
        public override PowerName PowerName => PowerName.MoreBombs;
        public override Dictionary<string, object>? Apply(Map map, int playerId, Dictionary<string, object>? parameters = null, int slotNum = -1) {
            base.Apply(map, playerId, parameters, slotNum);
            foreach (var activePowerUp in map.PlayerInfos[playerId].ActivePowerUps) {
                if (activePowerUp.PowerType == PowerName.MoreBombs) {
                    activePowerUp.StartTime = DateTime.Now;
                    return new Dictionary<string, object> {
                        { "playerId", playerId },
                        { "needToChange", false }
                    };
                }
            }

            map.PlayerInfos[playerId].ActivePowerUps.Add(new ActivePowerUp(PowerName.MoreBombs, DateTime.Now));
            return new Dictionary<string, object> {
                { "playerId", playerId },
                { "needToChange", true }
            };
        }
    }
}