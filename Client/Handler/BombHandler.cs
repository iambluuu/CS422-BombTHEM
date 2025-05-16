using System;
using System.Collections.Generic;
using System.Text.Json;
using Client.Component;
using Client.PowerUps;
using Microsoft.Xna.Framework;
using Shared;

namespace Client.Handler {

    public class BombHandler(MapRenderInfo map) : Handler {
        public void Handle(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.BombPlaced:
                    BombPlaced(message);
                    break;
                case ServerMessageType.BombExploded:
                    BombExploded(message);
                    break;
                default:
                    throw new Exception("Cannot handle bomb message: {type}");
            }
        }

        internal void BombPlaced(NetworkMessage message) {
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);
            int byPlayerId = int.Parse(message.Data["byPlayerId"]);
            BombType type = Enum.Parse<BombType>(message.Data["type"]);
            bool isCounted = bool.Parse(message.Data["isCounted"]);

            map.BombPlaced(x, y, type, byPlayerId, isCounted);
        }

        internal void BombExploded(NetworkMessage message) {
            int x = int.Parse(message.Data["x"]);
            int y = int.Parse(message.Data["y"]);
            string[] positions = message.Data["positions"].Split(';');
            int byPlayerId = int.Parse(message.Data["byPlayerId"]);
            bool isCounted = bool.Parse(message.Data["isCounted"]);

            map.BombExploded(x, y, positions, byPlayerId, isCounted);
        }
    }
}