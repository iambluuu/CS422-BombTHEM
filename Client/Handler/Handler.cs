using System;
using System.Collections.Generic;
using Shared;

namespace Client.Handler {
    public static class HandlerFactory {
        private readonly static Dictionary<ServerMessageType, IHandler> _handlers = new();
        public static IHandler CreateHandler(MapRenderInfo mapRenderInfo, ServerMessageType type) {
            if (_handlers.TryGetValue(type, out IHandler value)) {
                return value;
            }

            IHandler handler = null;
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
            _handlers[type] = handler;
            return handler;
        }
    }
    public interface IHandler {
        public virtual void Handle(NetworkMessage message) { }
    }
}