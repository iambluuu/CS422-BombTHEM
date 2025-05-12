using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client {
    public class VFXNode : SpriteNode {
        private readonly int _frameCount;
        private readonly bool _isLooping;
        private readonly bool _isInfinite;
        private readonly float _duration;

        private int _currentFrame;
        private float _frameTime;
        private float _elapsedFrameTime;
        private float _elapsedTime;

        public VFXNode(Texture2D texture, Vector2 size, int frameCount, float frameTime = 0.1f, float duration = 1f, bool isLooping = false, bool isInfinite = false) : base(texture, size) {
            _frameTime = frameTime;
            _elapsedFrameTime = 0f;
            _currentFrame = 0;
            _frameCount = frameCount;
            _isLooping = isLooping;
            _duration = duration;
            _isInfinite = isInfinite;
        }

        protected override void UpdateCurrent(GameTime gameTime) {
            base.UpdateCurrent(gameTime);

            _elapsedFrameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!_isInfinite) {
                _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            if (_elapsedFrameTime >= _frameTime) {
                _elapsedFrameTime = 0f;
                _currentFrame = _currentFrame + 1;
                if (_currentFrame >= _frameCount) {
                    if (!_isLooping) {
                        DetachSelf();
                    } else {
                        _currentFrame = 0;
                    }
                }
            }

            if (_elapsedTime >= _duration) {
                DetachSelf();
            }
        }

        public void OnRemove() {
            DetachSelf();
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
            Vector2 textureScale = new Vector2(Math.Min(_size.X / frameWidth, _size.Y / frameHeight));

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
