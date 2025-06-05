using System;

using Shared;
using Client.Scene;
using Shared.PacketWriter;

namespace Client.Handler {

    public class ItemHandler(MapRenderInfo map) : IHandler {
        public void Handle(NetworkMessage message) {
            switch ((ServerMessageType)message.Type.Name) {
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
            PowerName powerUpType = message.Data[(byte)ServerParams.PowerUpType] is byte b ? (PowerName)b : PowerName.None;
            int x = message.Data[(byte)ServerParams.X] as ushort? ?? -1;
            int y = message.Data[(byte)ServerParams.Y] as ushort? ?? -1;

            if (powerUpType == PowerName.None || x < 0 || y < 0) {
                Console.WriteLine($"Invalid item spawn data: {message.Data}");
                return;
            }

            map.ItemSpawned(x, y, powerUpType);
        }

        private void ItemExpired(NetworkMessage message) {
            int x = message.Data[(byte)ServerParams.X] as ushort? ?? -1;
            int y = message.Data[(byte)ServerParams.Y] as ushort? ?? -1;
            if (x < 0 || y < 0) {
                Console.WriteLine($"Invalid item expiration data: {message.Data}");
                return;
            }
            map.ItemExpired(x, y);
        }

        private void ItemPickedUp(NetworkMessage message) {
            int playerId = message.Data[(byte)ServerParams.PlayerId] as int? ?? -1;
            int x = message.Data[(byte)ServerParams.X] as ushort? ?? -1;
            int y = message.Data[(byte)ServerParams.Y] as ushort? ?? -1;
            PowerName powerType = message.Data[(byte)ServerParams.PowerUpType] is byte b ? (PowerName)b : PowerName.None;

            if (playerId == -1 || x < 0 || y < 0 || powerType == PowerName.None) {
                Console.WriteLine($"Invalid item pickup data: {message.Data}");
                return;
            }
            map.ItemPickedUp(playerId, x, y, powerType);
        }
    }
}