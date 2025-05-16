using System;
using System.Collections.Generic;
using Shared;

namespace Client.Handler {
    public static class HandlerFactory {
        private static Dictionary<ServerMessageType, Handler> _handlers;
        public static Handler CreateHandler(MapRenderInfo mapRenderInfo, ServerMessageType type) {
            if (_handlers.ContainsKey(type)) {
                return _handlers[type];
            }

            Handler handler = null;
            switch (type) {
                case ServerMessageType.BombExploded: {
                        // handler = new BombHandler();
                    }
                    break;
                default:
                    throw new Exception($"No handler available for {type}");
            }
            return _handlers[type] = handler;
        }
    }
    public abstract class Handler(MapRenderInfo mapRenderInfo) {
        protected MapRenderInfo mapRenderInfo = mapRenderInfo;

        public virtual void Handle(NetworkMessage message) { }
    }
}