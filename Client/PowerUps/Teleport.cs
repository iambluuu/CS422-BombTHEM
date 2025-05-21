using System;
using System.Collections.Generic;

using Client.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

namespace Client.PowerUps {
    public class Teleport(MapRenderInfo map) : PowerUp(map) {
        public override PowerName PowerName => PowerName.Teleport;
        public override void Apply(Dictionary<string, object> parameters, int slotNum) {
            base.Apply(parameters, slotNum);

            int x = int.Parse(parameters["x"].ToString());
            int y = int.Parse(parameters["y"].ToString());
            int oldX = int.Parse(parameters["oldX"].ToString());
            int oldY = int.Parse(parameters["oldY"].ToString());
            int playerId = int.Parse(parameters["playerId"].ToString());

            map.AddEnvVFX(oldX, oldY, PowerName.Teleport);
            map.AddPlayerVFX(playerId, PowerName.Teleport);
            map.TeleportPlayer(playerId, x, y);
            // var playerNodes = parameters["playerNodes"] as Dictionary<int, PlayerNode> ?? throw new ArgumentException("playerNodes must be a dictionary of PlayerNode.");
            // var vfxLayer = parameters["vfxLayer"] as SceneNode ?? throw new ArgumentException("vfxLayer must be a LayerNode.");
            // var map = parameters["map"] as Map ?? throw new ArgumentException("map must be a Map.");
            // PlayerNode target = playerNodes[playerId];

            // map.SetPlayerPosition(playerId, x, y);
            // target.TeleportTo(new Vector2(y * GameValues.TILE_SIZE, x * GameValues.TILE_SIZE));

            // AnimatedNode log = new(TextureHolder.Get("Item/Wood", new Rectangle(16, 0, 16, 16)), new Vector2(GameValues.TILE_SIZE * 0.8f, GameValues.TILE_SIZE * 0.8f)) {
            //     Position = new Vector2((oldY + 0.1f) * GameValues.TILE_SIZE, oldX * GameValues.TILE_SIZE),
            // };
            // log.AddAnimation(AnimationFactory.CreateLaunchAnimation(log, GameValues.TILE_SIZE * 0.6f, 0.6f));
            // VFXNode smokeEffect = new(TextureHolder.Get("Effect/Smoke"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE), 6, 0.1f) {
            //     Position = new Vector2(oldY * GameValues.TILE_SIZE, oldX * GameValues.TILE_SIZE),
            // };
            // vfxLayer.AttachChild(smokeEffect);
            // vfxLayer.AttachChild(log);

            // VFXNode landingEffect = new(TextureHolder.Get("Effect/SmokeCircular"), new Vector2(GameValues.TILE_SIZE * 1.2f, GameValues.TILE_SIZE), 8, 0.1f) {
            //     Position = new Vector2(-0.1f * GameValues.TILE_SIZE, 0.8f * GameValues.TILE_SIZE),
            // };
            // target.AttachChild(landingEffect);
        }

        // public override void Remove(SceneNode target) {
        //     if (target is not PlayerNode) {
        //         throw new ArgumentException("Target must be a PlayerNode.");
        //     }

        // }
    }
}