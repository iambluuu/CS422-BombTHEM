using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

namespace Client.PowerUps {
    public class Shield : PowerUp {
        private static Dictionary<PlayerNode, VFXNode> _activeEffects = new(); // Current Players with Shield

        public override void Apply(Dictionary<string, object> parameters) {
            base.Apply(parameters);
            bool needToChange = Boolean.Parse(parameters["needToChange"].ToString());
            if (!needToChange) {
                return;
            }

            int playerId = int.Parse(parameters["playerId"].ToString());
            var playerNodes = parameters["playerNodes"] as Dictionary<int, PlayerNode> ?? throw new ArgumentException("playerNodes must be a dictionary of PlayerNode.");
            PlayerNode target = playerNodes[playerId];
            VFXNode vfx = new(TextureHolder.Get("Effect/Shield"), new Vector2(GameValues.TILE_SIZE * 1.2f, GameValues.TILE_SIZE * 2), 6, 0.1f, isLooping: true, isInfinite: true) {
                Position = new Vector2(-0.1f * GameValues.TILE_SIZE, 0),
            };
            target.AttachChild(vfx);
            _activeEffects.Add(target, vfx);
        }

        public override void Remove(SceneNode target) {
            if (target is not PlayerNode) {
                throw new ArgumentException("Target must be a PlayerNode.");
            }

            if (_activeEffects.TryGetValue((PlayerNode)target, out VFXNode vfx)) {
                target.DetachChild(vfx);
                _activeEffects.Remove((PlayerNode)target);
            }
        }
    }
}