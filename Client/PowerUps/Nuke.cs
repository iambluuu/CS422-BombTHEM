using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Server;
using Shared;

namespace Client.PowerUps {
    public class Nuke : PowerUp {
        private static Dictionary<PlayerNode, (VFXNode, PlayerIngameInfo)> _activeEffects = new(); // Current Players with Nuke

        public override void Apply(Dictionary<string, object> parameters) {
            base.Apply(parameters);
            bool needToChange = Boolean.Parse(parameters["needToChange"].ToString());
            if (!needToChange) {
                return;
            }

            var map = parameters["map"] as Map ?? throw new ArgumentException("map must be a Map object.");
            var bombNodes = parameters["bombNodes"] as Dictionary<(int, int), BombNode> ?? throw new ArgumentException("bombNodes must be a dictionary of BombNode.");
            var bombLayer = parameters["bombLayer"] as SceneNode ?? throw new ArgumentException("bombLayer must be a Layer object.");
            int x = int.Parse(parameters["x"].ToString());
            int y = int.Parse(parameters["y"].ToString());
            
            PlantBomb(map, bombLayer, bombNodes, x, y);

            // playerInfo.ActivePowerUps.Add(new ActivePowerUp(PowerName.Nuke, DateTime.Now));

            // PlayerNode target = playerNodes[playerId];
            // VFXNode vfx = new(TextureHolder.Get("Effect/Circle"), new Vector2(GameValues.TILE_SIZE * 1.5f, GameValues.TILE_SIZE * 2), 4, 0.1f, isLooping: true, isInfinite: true) {
            //     Position = new Vector2(-0.25f * GameValues.TILE_SIZE, -0.25f * GameValues.TILE_SIZE),
            // };
            // target.AttachChild(vfx);
            // _activeEffects.Add(target, (vfx, playerInfo));
        }

        private void PlantBomb(Map map, SceneNode bombLayer, Dictionary<(int, int), BombNode> bombNodes, int x, int y) {
            map.AddBomb(x, y, BombType.Nuke);
            var bombNode = BombNodeFactory.CreateNode(BombType.Nuke);
            bombNode.Position = new Vector2(x * GameValues.TILE_SIZE, y * GameValues.TILE_SIZE);
            bombNodes.Add((x, y), bombNode);
            bombLayer.AttachChild(bombNode);
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