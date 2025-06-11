using System;
using System.Collections.Generic;
using System.Text.Json;

using Shared;
using Client.PowerUps;
using Client.Scene;
using Shared.PacketWriter;
using Client.Network;

namespace Client.Handler {

    public class PowerUpHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            switch ((ServerMessageType)message.Type.Name) {
                case ServerMessageType.PowerUpUsed:
                    PowerUpUsed(message);
                    break;
                case ServerMessageType.PowerUpExpired:
                    PowerUpExpired(message);
                    break;
                default:
                    throw new Exception($"Cannot handle item message: {message.Type.Name}");
            }
        }

        private void PowerUpUsed(NetworkMessage message) {
            int slotNum = message.Data[(byte)ServerParams.SlotNum] as byte? ?? -1;
            int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;

            if (playerId == NetworkManager.Instance.ClientId) {
                map.UnlockPowerSlot(slotNum);
            }

            if (message.Data.TryGetValue((byte)ServerParams.Invalid, out var invalid) && invalid is bool isInvalid && isInvalid) {
                return;
            }
            
            PowerName powerUpType = message.Data[(byte)ServerParams.PowerUpType] is byte b ? (PowerName)b : PowerName.None;

            PowerUp powerUp = PowerUpFactory.CreatePowerUp(powerUpType, map);
            powerUp.Apply(message.Data, slotNum);
        }

        private void PowerUpExpired(NetworkMessage message) {
            int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
            PowerName powerType = message.Data[(byte)ServerParams.PowerUpType] is byte b ? (PowerName)b : PowerName.None;

            if (playerId == -1 || powerType == PowerName.None) {
                throw new Exception("Invalid power-up expiration data received.");
            }

            map.PowerUpExpired(playerId, powerType);
        }
    }
}