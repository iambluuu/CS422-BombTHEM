using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

namespace Client.PowerUps {
    public class Teleport : PowerUp {
        public override void Apply(Dictionary<string, object> parameters) {
            base.Apply(parameters);

            int x = int.Parse(parameters["x"].ToString());
            int y = int.Parse(parameters["y"].ToString());
            int oldX = int.Parse(parameters["oldX"].ToString());
            int oldY = int.Parse(parameters["oldY"].ToString());
            int playerId = int.Parse(parameters["playerId"].ToString());
            var playerNodes = parameters["playerNodes"] as Dictionary<int, PlayerNode> ?? throw new ArgumentException("playerNodes must be a dictionary of PlayerNode.");
            var vfxLayer = parameters["vfxLayer"] as SceneNode ?? throw new ArgumentException("vfxLayer must be a LayerNode.");

            PlayerNode target = playerNodes[playerId];
            VFXNode log = new(TextureHolder.Get("Texture/Effect/Teleport"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE), 6, 0.1f) {
                Position = new Vector2(oldX * GameValues.TILE_SIZE, oldY * GameValues.TILE_SIZE),
            };
            VFXNode smokeEffect = new(TextureHolder.Get("Texture/Effect/Smoke"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE), 6, 0.1f, isLooping: true, isInfinite: true) {
                Position = new Vector2(oldX * GameValues.TILE_SIZE, oldY * GameValues.TILE_SIZE),
            };
            vfxLayer.AttachChild(log);
            vfxLayer.AttachChild(smokeEffect);

            VFXNode landingEffect = new(TextureHolder.Get("Texture/Effect/Teleport"), new Vector2(GameValues.TILE_SIZE * 1.2f, GameValues.TILE_SIZE), 6, 0.1f, isLooping: true, isInfinite: true) {
                Position = new Vector2(-0.1f * GameValues.TILE_SIZE, 0.8f * GameValues.TILE_SIZE),
            };
            target.AttachChild(landingEffect);
        }

        public override void Remove(SceneNode target) {
            if (target is not PlayerNode) {
                throw new ArgumentException("Target must be a PlayerNode.");
            }

        }
    }
}