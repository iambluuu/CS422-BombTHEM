using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Shared;
using Client.ContentHolder;

namespace Client.Scene {
    public static class BombNodeFactory {
        public static BombNode CreateNode(BombType type) {
            switch (type) {
                case BombType.Nuke:
                    return new NukeBombNode(TextureHolder.Get("Item/Bomb"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE));
                case BombType.Normal:
                default:
                    return new BombNode(TextureHolder.Get("Item/Bomb"), new Vector2(GameValues.TILE_SIZE, GameValues.TILE_SIZE));
            }
        }
    }

    public class BombNode : SpriteNode {
        protected DateTime _placeTime;
        protected bool _flashOn = false;
        protected double _lastFlashTime = 0;

        protected const double startFlashingMs = 1000;
        protected const double maxDurationMs = 2000;
        protected const double minInterval = 30;
        protected const double maxInterval = 300;
        private readonly Texture2D _invertedTexture;

        public BombNode(Texture2D texture, Vector2 size) : base(texture, size) {
            _placeTime = DateTime.Now;
            _invertedTexture = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            for (int i = 0; i < data.Length; i++) {
                if (data[i].A == 0)
                    continue;
                data[i] = new Color(1 - data[i].R / 255f, 1 - data[i].G / 255f, 1 - data[i].B / 255f);
            }

            _invertedTexture.SetData(data);
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            DateTime currentTime = DateTime.Now;
            double elapsedTime = (currentTime - _placeTime).TotalMilliseconds;
            double interval;

            if (elapsedTime < startFlashingMs) {
                interval = double.PositiveInfinity;
            } else {
                double progress = (elapsedTime - startFlashingMs) / (maxDurationMs - startFlashingMs);
                progress = Math.Clamp(progress, 0, 1);
                interval = minInterval + (maxInterval - minInterval) * Math.Pow(1 - progress, 3);
            }

            if (elapsedTime - _lastFlashTime >= interval) {
                _flashOn = !_flashOn;
                _lastFlashTime = elapsedTime;
            }

            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);

            Vector2 textureScale = new Vector2(_size.X / _texture.Width, _size.Y / _texture.Height);

            if (!_flashOn) {
                spriteBatch.Draw(
                    _texture,
                    position,
                    null,
                    Color.White,
                    rotation,
                    _origin,
                    scale * textureScale,
                    SpriteEffects.None,
                    0f
                );
            } else {
                spriteBatch.Draw(
                    _invertedTexture,
                    position,
                    null,
                    Color.White,
                    rotation,
                    _origin,
                    scale * textureScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }

    public class NukeBombNode : BombNode {
        public NukeBombNode(Texture2D texture, Vector2 size) : base(texture, size) {
            // Additional initialization for NukeBombNode if needed
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);

            Vector2 textureScale = new Vector2(_size.X / _texture.Width, _size.Y / _texture.Height);

            float t = (float)((DateTime.Now - _placeTime).TotalMilliseconds / maxDurationMs);
            float intensity = (float)Math.Clamp(t, 0, 1);
            float pulseScale = (float)(0.6 + Math.Abs(0.8 * intensity * Math.Sin(50 * Math.Sqrt(intensity) * t)));
            Vector2 pulseOffset = new Vector2(
                (GameValues.TILE_SIZE - _texture.Width * textureScale.X * pulseScale) / 2,
                (GameValues.TILE_SIZE - _texture.Height * textureScale.Y * pulseScale) / 2
            );

            spriteBatch.Draw(
                _texture,
                position + pulseOffset,
                null,
                new Color(1f, 0.3f + (1 - intensity) * 0.4f, 0.3f + (1 - intensity) * 0.4f),
                rotation,
                _origin,
                scale * textureScale * pulseScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
