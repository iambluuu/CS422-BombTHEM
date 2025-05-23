using System;
using System.Collections.Generic;
using System.Text.Json;

using Shared;
using Client.PowerUps;
using Client.Scene;

namespace Client.Handler {

    public class PowerUpHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.PowerUpUsed:
                    PowerUpUsed(message);
                    break;
                case ServerMessageType.PowerUpExpired:
                    PowerUpExpired(message);
                    break;
                default:
                    throw new Exception("Cannot handle power-up message: {type}");
            }
        }

        private void PowerUpUsed(NetworkMessage message) {
            int slotNum = int.Parse(message.Data["slotNum"]);
            map.UnlockPowerSlot(slotNum);
            // Console.WriteLine($"PowerUpUsed: {slotNum} is unlocked");
            if (message.Data.TryGetValue("invalid", out var invalid) && bool.Parse(invalid.ToString())) {
                return;
            }
            string powerUpType = message.Data["powerUpType"];
            Dictionary<string, object> parameters = message.Data["parameters"] != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(message.Data["parameters"]) : new Dictionary<string, object>();
            PowerUp powerUp = PowerUpFactory.CreatePowerUp(Enum.Parse<PowerName>(powerUpType), map);
            powerUp.Apply(parameters, slotNum);
        }

        private void PowerUpExpired(NetworkMessage message) {
            int playerId = int.Parse(message.Data["playerId"]);
            PowerName powerType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
            map.PowerUpExpired(playerId, powerType);
        }
    }
}