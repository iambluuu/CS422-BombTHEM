using Shared;

namespace Server.PowerHandler {
    public class NukeHandler : PowerUpHandler {
        public override Dictionary<string, object>? Apply(Map map, int playerId) {
            if (!map.UsePowerUp(playerId, PowerName.Nuke)) {
                return null;
            }

            foreach (var activePowerUp in map.PlayerInfos[playerId].ActivePowerUps) {
                Console.WriteLine($"Active power: {activePowerUp.PowerType}");
                if (activePowerUp.PowerType == PowerName.Nuke) {
                    activePowerUp.StartTime = DateTime.Now;
                    return null;
                }
            }

            map.PlayerInfos[playerId].ActivePowerUps.Add(new ActivePowerUp(PowerName.Nuke, DateTime.Now));
            return new Dictionary<string, object> {
                { "playerId", playerId },
                { "needToChange", true }
            };
        }
    }
}