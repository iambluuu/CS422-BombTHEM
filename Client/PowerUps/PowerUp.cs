using System;
using System.Collections.Generic;
using Shared;

namespace Client.PowerUps {
    public static class PowerUpFactory {
        private static readonly Dictionary<PowerName, PowerUp> _powerUps = new();

        public static PowerUp CreatePowerUp(PowerName type, MapRenderInfo map) {
            if (_powerUps.TryGetValue(type, out var powerUp)) {
                return powerUp;
            }

            PowerUp newPower = type switch {
                // PowerName.Ghost => new (),
                PowerName.Nuke => new Nuke(map),
                PowerName.MoreBombs => new MoreBombs(map),
                PowerName.Teleport => new Teleport(map),
                PowerName.Shield => new Shield(map),
                _ => throw new ArgumentException($"Unknown power-up type: {type}")
            };
            return newPower;
        }
    }

    public abstract class PowerUp {
        public static PowerUp Instance { get; private set; }
        public abstract PowerName PowerName { get; }
        public virtual void Apply(Dictionary<string, object> parameters) {
            try {
                if (parameters == null || parameters.Count == 0) {
                    throw new ArgumentException("Parameters cannot be null or empty.");
                }
            } catch (ArgumentException e) {
                Console.WriteLine(e.Message);
            }
        }

        public virtual void Use() { }

        // public virtual void Remove(SceneNode target) {
        //     // Default implementation does nothing
        // }
    }
}