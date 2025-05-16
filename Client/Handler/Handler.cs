using System;
using Shared;

namespace Client.Handler {
    public static class HandlerFactory {
        public static Handler CreateHandler(MapRenderInfo mapRenderInfo, string type) {
            return type switch {
                // "PlayerMoved" => new PlayerMovedHandler(mapRenderInfo),
                // "PlayerLeft" => new PlayerLeftHandler(mapRenderInfo),
                // "GameLeft" => new GameLeftHandler(mapRenderInfo),
                // "BombPlaced" => new BombPlacedHandler(mapRenderInfo),
                // "BombExploded" => new BombExplodedHandler(mapRenderInfo),
                // "PlayerDied" => new PlayerDiedHandler(mapRenderInfo),
                // "GameStopped" => new GameStoppedHandler(mapRenderInfo),
                _ => throw new NotImplementedException($"No handler for {type}"),
            };
        }
    }
    public abstract class Handler(MapRenderInfo mapRenderInfo) {

        protected MapRenderInfo mapRenderInfo = mapRenderInfo;

        public virtual void Handle(NetworkMessage message) { }
    }
}