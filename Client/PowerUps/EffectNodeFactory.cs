using System;
using Client.Animation;
using Client.Component;
using Microsoft.Xna.Framework;
using Shared;

namespace Client.PowerUps {
    public static class EffectNodeFactory {
        public static SceneNode CreatePlayerEffect(PowerName powerName) {
            return powerName switch {
                PowerName.Shield => new VFXNode(TextureHolder.Get("Effect/Shield"), new Vector2(GameValues.TILE_SIZE * 1.2f, GameValues.TILE_SIZE), 6, 0.1f, isLooping: true, isInfinite: true) {
                    Position = new Vector2(-0.1f * GameValues.TILE_SIZE, 0),
                },
                PowerName.MoreBombs => new VFXNode(TextureHolder.Get("Effect/Aura"), new Vector2(GameValues.TILE_SIZE * 1.2f, GameValues.TILE_SIZE * 2), 5, 0.1f, isLooping: true, isInfinite: true) {
                    Position = new Vector2(-0.1f * GameValues.TILE_SIZE, 0),
                },
                PowerName.Teleport => new VFXNode(TextureHolder.Get("Effect/SmokeCircular"), new Vector2(GameValues.TILE_SIZE * 1.2f, GameValues.TILE_SIZE), 8, 0.1f) {
                    Position = new Vector2(-0.1f * GameValues.TILE_SIZE, 0.8f * GameValues.TILE_SIZE),
                },
                _ => throw new ArgumentOutOfRangeException(nameof(powerName), powerName, null)
            };
        }

        public static SceneNode CreateEnvEffect(PowerName powerName, int x, int y) {
            return powerName switch {
                PowerName.Teleport => TeleportEnvEffect(x, y),
                _ => throw new ArgumentOutOfRangeException(nameof(powerName), powerName, null)
            };
        }

        internal static SceneNode TeleportEnvEffect(int x, int y) {
            SceneNode tempNode = new SceneNode() {
                Position = new Vector2(y * GameValues.TILE_SIZE, x * GameValues.TILE_SIZE),
            };
            AnimatedNode log = new(TextureHolder.Get("Item/Wood", new Rectangle(16, 0, 16, 16)), new Vector2(GameValues.TILE_SIZE * 0.8f, GameValues.TILE_SIZE * 0.8f)) {
                Position = new Vector2(0.1f * GameValues.TILE_SIZE, 0),
            };
            log.AddAnimation(AnimationFactory.CreateLaunchAnimation(log, GameValues.TILE_SIZE * 0.6f, 0.6f));
            VFXNode smokeEffect = new(TextureHolder.Get("Effect/Smoke"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE), 6, 0.1f);

            tempNode.AttachChild(smokeEffect);
            tempNode.AttachChild(log);
            return tempNode;
        }
    }
}