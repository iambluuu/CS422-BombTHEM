using Shared;

namespace Server.PowerHandler {
    public class NukeHandler : PowerUpHandler {
        public override Dictionary<string, object>? Apply(Map map, int playerId, Dictionary<string, object>? parameters = null) {
            if (parameters == null) {
                return null;
            }

            if (!parameters.TryGetValue("x", out var xObj) || !parameters.TryGetValue("y", out var yObj)) {
                return null;
            }

            if (!int.TryParse(xObj.ToString(), out int x) || !int.TryParse(yObj.ToString(), out int y)) {
                return null;
            }

            if (!map.AddBomb(x, y, BombType.Nuke, playerId)) {
                return null;
            }

            return new() {
                { "x", x.ToString() },
                { "y", y.ToString() },
                { "type", BombType.Nuke.ToString() },
                { "byPlayerId", playerId.ToString() },
                { "isCounted", "False" }
            };
        }
    }
}