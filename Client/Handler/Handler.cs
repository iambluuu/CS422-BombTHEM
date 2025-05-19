using System;
using System.Collections.Generic;
using Shared;

namespace Client.Handler {
    public static class HandlerFactory {
        private static Dictionary<ServerMessageType, Handler> _handlers = new();
        public static Handler CreateHandler(MapRenderInfo mapRenderInfo, ServerMessageType type) {
            if (_handlers.ContainsKey(type)) {
                return _handlers[type];
            }

            Handler handler = null;
            switch (type) {
                case ServerMessageType.PlayerMoved:
                case ServerMessageType.PlayerDied:
                case ServerMessageType.PlayerLeft:
                case ServerMessageType.GameLeft: {
                        handler = new PlayerHandler(mapRenderInfo);
                        break;
                    }
                case ServerMessageType.GameInfo: {
                        handler = new GameInfoHandler(mapRenderInfo);
                        break;
                    }
                case ServerMessageType.BombPlaced:
                case ServerMessageType.BombExploded: {
                        handler = new BombHandler(mapRenderInfo);
                        break;
                    }
                case ServerMessageType.ItemSpawned:
                case ServerMessageType.ItemPickedUp:
                case ServerMessageType.ItemExpired: {
                        handler = new ItemHandler(mapRenderInfo);
                        break;
                    }

                case ServerMessageType.PowerUpUsed:
                case ServerMessageType.PowerUpExpired: {
                        handler = new PowerUpHandler(mapRenderInfo);
                        break;
                    }
            }
            return _handlers[type] = handler;
        }
    }
    public interface Handler {
        public virtual void Handle(NetworkMessage message) { }
    }
}