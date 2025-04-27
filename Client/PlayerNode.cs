using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Shared;

namespace Client {
    public class PlayerNode : MoveableSpriteNode {
        private readonly int _frameCount = 4;
        private int _currentFrame;
        private int _lastFrame;
        private float _frameTime;
        private float _elapsedFrameTime;
        private Direction _currentDirection;

        public PlayerNode(Texture2D texture, Vector2 size, float frameTime = 0.15f)
            : base(texture, size) {
            _frameTime = frameTime;
            _elapsedFrameTime = 0f;
            _currentFrame = 0;
            _lastFrame = -1;
            _currentDirection = Direction.Down;
        }

        public void SetDirection(Direction dir) {
            if (_currentDirection != dir) {
                _currentDirection = dir;
                _currentFrame = 0;
                _elapsedFrameTime = 0f;
            }
        }

        public void MoveTo(Vector2 target, Direction direction, float durationSeconds) {
            base.MoveTo(target, durationSeconds);
            SetDirection(direction);
        }

        protected override void UpdateCurrent(GameTime gameTime) {
            base.UpdateCurrent(gameTime);
            if (Moving) {
                if (_lastFrame != -1) {
                    _currentFrame = _lastFrame;
                    _lastFrame = -1;
                }

                _elapsedFrameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_elapsedFrameTime >= _frameTime) {
                    _elapsedFrameTime = 0f;
                    _currentFrame = (_currentFrame + 1) % _frameCount;
                }
            } else {
                _lastFrame = _currentFrame;
                _currentFrame = 0;
            }
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);

            int frameWidth = _texture.Width / 4;
            int frameHeight = _texture.Height / _frameCount;
            Vector2 textureScale = new Vector2(_size.X / frameWidth, _size.Y / frameHeight);

            Rectangle sourceRect = new Rectangle(
                (int)_currentDirection * frameWidth,
                _currentFrame * frameHeight,
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
