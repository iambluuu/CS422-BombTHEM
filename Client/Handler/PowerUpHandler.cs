using System;
using System.Collections.Generic;
using System.Text.Json;
using Client.Component;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Shared;

namespace Client.Handler {

    public class PowerUpHandler(MapRenderInfo map) : Handler {
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

        internal void PowerUpUsed(NetworkMessage message) {
            string powerUpType = message.Data["powerUpType"];
            Dictionary<string, object> parameters = message.Data["parameters"] != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(message.Data["parameters"]) : new Dictionary<string, object>();

            PowerUp powerUp = PowerUpFactory.GetPowerUp(Enum.Parse<PowerName>(powerUpType));
            powerUp.Apply(parameters);
        }

        internal void PowerUpExpired(NetworkMessage message) {
            int playerId = int.Parse(message.Data["playerId"]);
            PowerName powerType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
            map.PowerUpExpired(playerId, powerType);
        }
    }
}