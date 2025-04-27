using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client {
    public class ExplosionNode : SpriteNode {
        private readonly int _frameCount = 9;
        private int _currentFrame;
        private float _frameTime;
        private float _elapsedFrameTime;

        public ExplosionNode(Texture2D texture, Vector2 size, float frameTime = 0.1f)
            : base(texture, size) {
            _frameTime = frameTime;
            _elapsedFrameTime = 0f;
            _currentFrame = 0;
        }

        protected override void UpdateCurrent(GameTime gameTime) {
            base.UpdateCurrent(gameTime);

            _elapsedFrameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_elapsedFrameTime >= _frameTime) {
                _elapsedFrameTime = 0f;
                _currentFrame = _currentFrame + 1;
                if (_currentFrame >= _frameCount) {
                    DetachSelf();
                }
            }
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            if (_currentFrame >= _frameCount) {
                return;
            }

            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);

            int frameWidth = _texture.Width / _frameCount;
            int frameHeight = _texture.Height;
            Vector2 textureScale = new Vector2(_size.X / frameWidth, _size.Y / frameHeight);

            Rectangle sourceRect = new Rectangle(
                _currentFrame * frameWidth,
                0,
                frameWidth,
                frameHeight
            );

            spriteBatch.Draw(
                _texture,
                position,
                sourceRect,
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
