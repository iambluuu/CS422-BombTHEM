using Shared;

namespace Server.PowerHandler {
    public class MoreBombsHandler : PowerUpHandler {
        public override Dictionary<string, object>? Apply(Map map, int playerId, Dictionary<string, object>? parameters = null) {
            foreach (var activePowerUp in map.PlayerInfos[playerId].ActivePowerUps) {
                if (activePowerUp.PowerType == PowerName.MoreBombs) {
                    activePowerUp.StartTime = DateTime.Now;
                    return null;
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