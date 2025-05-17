using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Server;
using Shared;

namespace Client.PowerUps {
    public class Nuke(MapRenderInfo map) : PowerUp {
        private static Dictionary<PlayerNode, (VFXNode, PlayerIngameInfo)> _activeEffects = new(); // Current Players with Nuke
        public override PowerName PowerName => PowerName.Nuke;

        public override void Use() {
            Position nearestCell = map.GetNearestCell();
            if (nearestCell != null) {
                if (map.BombCount >= GameplayConfig.MaxBombs && !map.HasActivePowerUp(PowerName.MoreBombs) && !map.LockTile(nearestCell.X, nearestCell.Y)) {
                    return;
                }

                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                    { "x", nearestCell.X.ToString() },
                    { "y", nearestCell.Y.ToString() },
                    { "type", BombType.Nuke.ToString() },
                }));
            }
        }

        // public override void Remove(SceneNode target) {
        //     if (target is not PlayerNode) {
        //         throw new ArgumentException("Target must be a PlayerNode.");
        //     }

        //     if (_activeEffects.TryGetValue((PlayerNode)target, out var mappedValue)) {
        //         target.DetachChild(mappedValue.Item1);
        //         var playerInfo = mappedValue.Item2;
        //         playerInfo.ExpireActivePowerUp(PowerName.Nuke);
        //         _activeEffects.Remove((PlayerNode)target);
        //     }
        // }
    }
}