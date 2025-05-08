using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using SharpDX.Direct2D1.Effects;

namespace Client.PowerUps {
    public class Shield : PowerUp {
        public static Shield Instance { get; }
        private static Dictionary<PlayerNode, VFXNode> _activeEffects = new();

        public override void Apply(Dictionary<string, object> parameters) {
            if (parameters == null || parameters.Count == 0) {
                throw new ArgumentException("Parameters cannot be null or empty.");
            }

            PlayerNode target = parameters["target"] as PlayerNode;
            if (target == null) {
                throw new ArgumentException("Target must be a PlayerNode.");
            }
            VFXNode vfx = new VFXNode(TextureHolder.Get("Texture/Effect/Shield"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE), 6, 0.1f, 5, true);
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