using System;
using System.Collections.Generic;
using Shared;

namespace Client.PowerUps {
    public static class PowerUpFactory {
        private static readonly Dictionary<PowerName, PowerUp> _powerUps = new();
        public static PowerUp GetPowerUp(PowerName type) {
            if (_powerUps.TryGetValue(type, out var powerUp)) {
                return powerUp;
            }

            powerUp = CreatePowerUp(type);
            _powerUps[type] = powerUp;
            return powerUp;
        }

        public static PowerUp CreatePowerUp(PowerName type) {
            return type switch {
                // PowerName.Ghost => new (),
                PowerName.Teleport => new Teleport(),
                PowerName.Shield => new Shield(),
                _ => throw new ArgumentException($"Unknown power-up type: {type}")
            };
        }
    }

    public abstract class PowerUp {
        public static PowerUp Instance { get; private set; }
        public virtual void Apply(Dictionary<string, object> parameters) {
            try {
                if (parameters == null || parameters.Count == 0) {
                    throw new ArgumentException("Parameters cannot be null or empty.");
                }
            } catch (ArgumentException e) {
                Console.WriteLine(e.Message);
            }
        }

        public virtual void Remove(SceneNode target) {
            // Default implementation does nothing
        }
    }
}