using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Server;
using Shared;

namespace Client.PowerUps {
    public class Nuke(MapRenderInfo map) : PowerUp(map) {
        public override PowerName PowerName => PowerName.Nuke;

        public override void Apply(Dictionary<string, object> parameters, int slotNum = -1) {
            int playerId = int.Parse(parameters["playerId"].ToString());
            if (playerId == NetworkManager.Instance.ClientId) {
                if (map.HasActivePowerUp(PowerName.Nuke)) {
                    // Console.WriteLine("Nuke already activated, slotNum: " + slotNum);
                    map.PowerUpUsed(slotNum);
                    return;
                }
                if (slotNum == -1) {
                    throw new ArgumentException("Slot number cannot be -1.");
                }
                map.ActivatePowerUp(slotNum);
                map.AddActivePowerUp(PowerName.Nuke);
            }
            map.AddPlayerVFX(int.Parse(playerId.ToString()), PowerName.Nuke);
        }

        public override Dictionary<string, object> Use() {
            // Position nearestCell = map.GetNearestCell();
            // if (nearestCell != null) {
            //     if (map.BombCount >= GameplayConfig.MaxBombs && !map.HasActivePowerUp(PowerName.MoreBombs) && !map.LockTile(nearestCell.X, nearestCell.Y)) {
            //         return null;
            //     }

            //     return new() {
            //         { "x", nearestCell.X.ToString() },
            //         { "y", nearestCell.Y.ToString() },
            //     };
            // }
            if (map.HasActivePowerUp(PowerName.Nuke)) {
                return null;
            }
            return new Dictionary<string, object>();
        }
    }
}