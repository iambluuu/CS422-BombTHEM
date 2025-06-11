
using Shared;
using Shared.PacketWriter;

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
        public abstract PowerName PowerName { get; }
        public static PowerUpHandler? Instance { get; private set; }
        public virtual Dictionary<byte, object>? Apply(ServerMap map, int playerId, Dictionary<string, object>? parameters = null, int slotNum = -1) {
            map.UsePowerUp(playerId, PowerName, slotNum);
            return null;
        }

        public virtual Dictionary<byte, object>? Use(ServerMap map, int playerId, Dictionary<string, object>? parameters = null) { return null; }
    }
}