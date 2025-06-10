using System;
using System.Collections.Generic;

using Shared;
using Client.Scene;
using Client.Network;
using Shared.PacketWriter;

namespace Client.PowerUps {
    public class Nuke(MapRenderInfo map) : PowerUp(map) {
        private readonly MapRenderInfo map = map;

        public override PowerName PowerName => PowerName.Nuke;

        public override void Apply(Dictionary<byte, object> parameters, int slotNum = -1) {
            int playerId = parameters[(byte)ServerParams.PlayerId] is int id ? id : throw new ArgumentException("PlayerId must be an integer.");
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
            map.AddPlayerVFX(playerId, PowerName.Nuke);
        }

        public override bool CanUse() {
            return !map.HasActivePowerUp(PowerName.Nuke);
        }
    }
}