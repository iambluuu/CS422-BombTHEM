using Shared;

namespace Server.PowerHandler {
    public class ShieldHandler : PowerUpHandler {
        public override Dictionary<string, object> Apply(Map map, int playerId) {
            if (!map.UsePowerUp(playerId, PowerName.Shield)) {
                return new Dictionary<string, object> {
                    { "playerId", playerId },
                    { "needToChange", false }
                };
            }

            // if already active, renew time and signal the client not to change anything
            foreach (var activePowerUp in map.PlayerInfos[playerId].ActivePowerUps) {
                if (activePowerUp.PowerType == PowerName.Shield) {
                    activePowerUp.StartTime = DateTime.Now;
                    return new Dictionary<string, object> {
                        { "playerId", playerId },
                        { "needToChange", false }
                    };
                }
            }

            // if not active, add it to the list and signal the client to display the shield
            map.PlayerInfos[playerId].ActivePowerUps.Add(new ActivePowerUp(PowerName.Shield, DateTime.Now));
            return new Dictionary<string, object> {
                { "playerId", playerId },
                { "needToChange", true }
            };
        }
    }
}