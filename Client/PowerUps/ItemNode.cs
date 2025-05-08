using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

namespace Client.PowerUps {
    public class ItemNode : SceneNode {
        private Texture2D _texture;
        private Vector2 _floatingOffset = new Vector2(0, 0);
        private const float _floatingSpeed = 2f;
        private const float _floatingAmplitude = 5f;

        public ItemNode(PowerName name) {
            // Constructor logic here
            _texture = TextureHolder.Get($"Texture/Power/{name}");
        }

        protected override void UpdateCurrent(GameTime gameTime) {
            // Update logic here
            _floatingOffset = new Vector2(0, (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * _floatingSpeed) * _floatingAmplitude);
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            position += _floatingOffset; // Apply floating effect
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);
            Vector2 textureScale = new Vector2((GameValues.TILE_SIZE * 0.7f) / _texture.Width, (GameValues.TILE_SIZE * 0.7f) / _texture.Height);

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