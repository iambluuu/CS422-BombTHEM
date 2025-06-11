using System;
using System.Collections.Generic;

using Shared;
using Client.Scene;
using Client.Audio;
using Shared.PacketWriter;

namespace Client.PowerUps {
    public class Teleport(MapRenderInfo map) : PowerUp(map) {
        private readonly MapRenderInfo map = map;

        public override PowerName PowerName => PowerName.Teleport;

        public override void Apply(Dictionary<byte, object> parameters, int slotNum) {
            base.Apply(parameters, slotNum);

            int x = parameters[(byte)ServerParams.X] as ushort? ?? throw new ArgumentException("X coordinate must be an integer.");
            int y = parameters[(byte)ServerParams.Y] as ushort? ?? throw new ArgumentException("Y coordinate must be an integer.");
            int oldX = parameters[(byte)ServerParams.OldX] as ushort? ?? throw new ArgumentException("Old X coordinate must be an integer.");
            int oldY = parameters[(byte)ServerParams.OldY] as ushort? ?? throw new ArgumentException("Old Y coordinate must be an integer.");
            int playerId = parameters[(byte)ServerParams.PlayerId] as int? ?? throw new ArgumentException("PlayerId must be an integer.");

            map.AddEnvVFX(oldX, oldY, PowerName.Teleport);
            map.AddPlayerVFX(playerId, PowerName.Teleport);
            map.TeleportPlayer(playerId, x, y);
            SoundPlayer.Play("Slash", 2f);
        }
    }
}