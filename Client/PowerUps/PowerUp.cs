using System;
using System.Collections.Generic;
using Shared;

namespace Client.PowerUps {
    public static class PowerUpFactory {
        public static PowerUp CreatePowerUp(PowerName type) {
            return type switch {
                // PowerName.Ghost => new (),
                // PowerName.Shield => new (),
                _ => throw new ArgumentException($"Unknown power-up type: {type}")
            };
        }
    }

    public abstract class PowerUp {
        public static PowerUp Instance { get; private set; }
        public virtual void Apply(Dictionary<string, object> parameters) {
            // Default implementation does nothing
        }

        public virtual void Remove(SceneNode target) {
            // Default implementation does nothing
        }
    }
}