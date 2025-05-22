using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

namespace Client.PowerUps {
    public class Shield(MapRenderInfo map) : PowerUp(map) {
        private readonly MapRenderInfo map = map;

        public override PowerName PowerName => PowerName.Shield;

        public override void Apply(Dictionary<string, object> parameters, int slotNum) {
            base.Apply(parameters, slotNum);
            bool needToChange = bool.Parse(parameters["needToChange"].ToString());
            if (!needToChange) {
                return;
            }

            int playerId = int.Parse(parameters["playerId"].ToString());
            if (playerId == NetworkManager.Instance.ClientId) {
                map.AddActivePowerUp(PowerName.Shield);
            }
            map.AddPlayerVFX(int.Parse(playerId.ToString()), PowerName.Shield);
        }

        // public override void Remove(SceneNode target) {
        //     if (target is not PlayerNode) {
        //         throw new ArgumentException("Target must be a PlayerNode.");
        //     }

        //     if (_activeEffects.TryGetValue((PlayerNode)target, out VFXNode vfx)) {
        //         target.DetachChild(vfx);
        //         _activeEffects.Remove((PlayerNode)target);
        //     }
        // }
    }
}