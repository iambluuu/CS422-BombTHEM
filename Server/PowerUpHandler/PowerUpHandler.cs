
using Shared;

namespace Server.PowerUpHandler {
    public static class PowerUpHandlerFactory {
        public static PowerUpHandler CreatePowerUpHandler(PowerName type) {
            return type switch {
                // PowerName.Ghost => new (),
                // PowerName.Shield => new (),
                _ => throw new ArgumentException($"Unknown power-up type: {type}")
            };
        }
    }

    public abstract class PowerUpHandler {
        public static PowerUpHandler? Instance { get; private set; }
        public virtual Dictionary<string, object> Apply(Map _map) {
            return new Dictionary<string, object>();
        }
    }
}