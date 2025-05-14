using Shared;

namespace Server.PowerHandler {
    public class MoreBombsHandler : PowerUpHandler {
        public override Dictionary<string, object>? Apply(Map map, int playerId) {
            if (!map.UsePowerUp(playerId, PowerName.MoreBombs)) {
                return null;
            }

            foreach (var activePowerUp in map.PlayerInfos[playerId].ActivePowerUps) {
                Console.WriteLine($"Active power: {activePowerUp.PowerType}");
                if (activePowerUp.PowerType == PowerName.MoreBombs) {
                    activePowerUp.StartTime = DateTime.Now;
                    return null;
                }
            }

            // if not active, add it to the list and signal the client to display the shield
            map.PlayerInfos[playerId].ActivePowerUps.Add(new ActivePowerUp(PowerName.MoreBombs, DateTime.Now));
            return new Dictionary<string, object> {
                { "playerId", playerId },
                { "needToChange", true }
            };
        }
    }
}