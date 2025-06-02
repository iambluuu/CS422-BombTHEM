using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Shared;
using Client.ContentHolder;

namespace Client.Scene {
    public class ItemNode : SceneNode {
        private Texture2D _texture;
        private Vector2 _floatingOffset = new Vector2(0, 0);
        private readonly Vector2 _size;
        private const float _floatingSpeed = 4f; // higher is faster
        private const float _floatingAmplitude = 5f;

        public ItemNode(PowerName name) {
            // Constructor logic here
            _texture = TextureHolder.Get($"Power/{name}");
            _size = new Vector2(GameValues.TILE_SIZE * 0.7f, GameValues.TILE_SIZE * 0.7f);
        }

        protected override void UpdateCurrent(GameTime gameTime) {
            // Update logic here
            _floatingOffset = new Vector2(0, (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * _floatingSpeed) * _floatingAmplitude);
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            position += _floatingOffset; // Apply floating effect
            position += (new Vector2(GameValues.TILE_SIZE) - _size) / 2; // Center the item
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);
            Vector2 textureScale = new Vector2(_size.X / _texture.Width, _size.Y / _texture.Height);

            spriteBatch.Draw(
                _texture,
                position,
                new Rectangle(0, 0, _texture.Width, _texture.Height),
                Color.White,
                rotation,
                Vector2.Zero,
                scale * textureScale,
                SpriteEffects.None,
                0f
            );
        }

    }
}