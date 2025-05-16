using System;
using System.Collections.Generic;
using System.Text.Json;
using Client.Component;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Shared;

namespace Client.Handler {

    public class ItemHandler(MapRenderInfo map) : Handler {
        public void Handle(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.ItemSpawned:
                    ItemSpawned(message);
                    break;
                case ServerMessageType.ItemExpired:
                    ItemExpired(message);
                    break;
                default:
                    throw new Exception("Cannot handle item message: {type}");
            }
        }

        internal void ItemSpawned(NetworkMessage message) {
            PowerName powerUpType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);
            map.ItemSpawned(x, y, powerUpType);
        }

        internal void ItemExpired(NetworkMessage message) {
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);
            map.ItemExpired(x, y);
        }
    }
}