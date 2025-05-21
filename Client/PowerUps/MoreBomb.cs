using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Server;
using Shared;

namespace Client.PowerUps {
    public class MoreBombs(MapRenderInfo map) : PowerUp(map) {
        private static Dictionary<PlayerNode, (VFXNode, PlayerIngameInfo)> _activeEffects = new(); // Current Players with More Bombs
        public override PowerName PowerName => PowerName.MoreBombs;
        public override void Apply(Dictionary<string, object> parameters, int slotNum = -1) {
            base.Apply(parameters, slotNum);
            bool needToChange = bool.Parse(parameters["needToChange"].ToString());
            if (!needToChange) {
                return;
            }
            int playerId = int.Parse(parameters["playerId"].ToString());
            if (playerId == NetworkManager.Instance.ClientId) {
                map.AddActivePowerUp(PowerName.MoreBombs);
            }

            map.AddPlayerVFX(int.Parse(playerId.ToString()), PowerName.MoreBombs);
            // int playerId = int.Parse(parameters["playerId"].ToString());
            // var playerNodes = parameters["playerNodes"] as Dictionary<int, PlayerNode> ?? throw new ArgumentException("playerNodes must be a dictionary of PlayerNode.");
            // var map = parameters["map"] as Map ?? throw new ArgumentException("map must be a Map object.");
            // var playerInfo = map.PlayerInfos[playerId] as PlayerIngameInfo ?? throw new ArgumentException("PlayerIngameInfo must be a PlayerIngameInfo object.");

            // playerInfo.ActivePowerUps.Add(new ActivePowerUp(PowerName.MoreBombs, DateTime.Now));

            // PlayerNode target = playerNodes[playerId];
            // VFXNode vfx = new(TextureHolder.Get("Effect/Aura"), new Vector2(GameValues.TILE_SIZE * 1.2f, GameValues.TILE_SIZE * 2), 5, 0.1f, isLooping: true, isInfinite: true) {
            //     Position = new Vector2(-0.1f * GameValues.TILE_SIZE, 0),
            // };
            // target.AttachChild(vfx);
            // _activeEffects.Add(target, (vfx, playerInfo));
        }

        // public override void Remove(SceneNode target) {
        //     if (target is not PlayerNode) {
        //         throw new ArgumentException("Target must be a PlayerNode.");
        //     }

        //     if (_activeEffects.TryGetValue((PlayerNode)target, out var mappedValue)) {
        //         target.DetachChild(mappedValue.Item1);
        //         var playerInfo = mappedValue.Item2;
        //         playerInfo.ExpireActivePowerUp(PowerName.MoreBombs);
        //         _activeEffects.Remove((PlayerNode)target);
        //     }
        // }
    }
}