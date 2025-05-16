using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Server;
using Shared;

namespace Client.PowerUps {
    public class Nuke : PowerUp {
        private static Dictionary<PlayerNode, (VFXNode, PlayerIngameInfo)> _activeEffects = new(); // Current Players with Nuke
        public override PowerName PowerName => PowerName.Nuke;
        public override void Apply(Dictionary<string, object> parameters) {
            base.Apply(parameters);
            bool needToChange = Boolean.Parse(parameters["needToChange"].ToString());
            if (!needToChange) {
                return;
            }

        }

        public override void Use() {
            
        }

        public override void Remove(SceneNode target) {
            if (target is not PlayerNode) {
                throw new ArgumentException("Target must be a PlayerNode.");
            }

            if (_activeEffects.TryGetValue((PlayerNode)target, out var mappedValue)) {
                target.DetachChild(mappedValue.Item1);
                var playerInfo = mappedValue.Item2;
                playerInfo.ExpireActivePowerUp(PowerName.Nuke);
                _activeEffects.Remove((PlayerNode)target);
            }
        }
    }
}