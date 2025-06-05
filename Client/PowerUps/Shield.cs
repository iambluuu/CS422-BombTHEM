using System.Collections.Generic;

using Shared;
using Client.Scene;
using Client.Network;
using Shared.PacketWriter;
using System;

namespace Client.PowerUps {
    public class Shield(MapRenderInfo map) : PowerUp(map) {
        private readonly MapRenderInfo map = map;

        public override PowerName PowerName => PowerName.Shield;

        public override void Apply(Dictionary<byte, object> parameters, int slotNum) {
            base.Apply(parameters, slotNum);
            bool needToChange = parameters[(byte)ServerParams.NeedToChange] is bool change ? change : throw new ArgumentException("needToChange must be a boolean.");
            if (!needToChange) {
                return;
            }

            int playerId = parameters[(byte)ServerParams.PlayerId] is int id ? id : throw new ArgumentException("PlayerId must be an integer.");
            if (playerId == NetworkManager.Instance.ClientId) {
                map.AddActivePowerUp(PowerName.Shield);
            }
            map.AddPlayerVFX(playerId, PowerName.Shield);
        }
    }
}