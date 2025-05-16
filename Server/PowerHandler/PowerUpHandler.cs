
using Shared;

namespace Server.PowerHandler {
    public static class PowerUpHandlerFactory {
        public static PowerUpHandler CreatePowerUpHandler(PowerName type) {
            return type switch {
                // PowerName.Ghost => new (),
                PowerName.Nuke => new NukeHandler(),
                PowerName.MoreBombs => new MoreBombsHandler(),
                PowerName.Teleport => new TeleportHandler(),
                PowerName.Shield => new ShieldHandler(),
                _ => throw new ArgumentException($"Unknown power-up type: {type}")
            };
        }
    }

    public abstract class PowerUpHandler {
        public static PowerUpHandler? Instance { get; private set; }
        public virtual Dictionary<string, object>? Apply(Map map, int playerId) {
            return null;
        }
    }
}