using System;
using System.Collections.Generic;
using System.Text.Json;
using Client.Component;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Shared;

namespace Client.Handler {

    public class ItemHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.ItemSpawned:
                    ItemSpawned(message);
                    break;
                case ServerMessageType.ItemExpired:
                    ItemExpired(message);
                    break;
                case ServerMessageType.ItemPickedUp:
                    ItemPickedUp(message);
                    break;
                default:
                    throw new Exception("Cannot handle item message: {type}");
            }
        }

        private void ItemSpawned(NetworkMessage message) {
            PowerName powerUpType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);
            map.ItemSpawned(x, y, powerUpType);
        }

        private void ItemExpired(NetworkMessage message) {
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);
            map.ItemExpired(x, y);
        }

        private void ItemPickedUp(NetworkMessage message) {
            int playerId = int.Parse(message.Data["playerId"]);
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);
            PowerName powerType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
            map.ItemPickedUp(playerId, x, y, powerType);
        }
    }
}