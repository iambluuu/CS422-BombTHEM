using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Shared;
using Client.ContentHolder;

namespace Client.Scene {
    public class PlayerNode : MoveableSpriteNode {
        private readonly int _frameCount = 4;
        private int _currentFrame;
        private int _lastFrame;
        private const float _frameTime = 0.1f;
        private float _elapsedFrameTime;
        private DateTime _lastDie = DateTime.MinValue;
        private Direction _currentDirection;

        public PlayerNode(Texture2D texture, Vector2 size, bool isLocalPlayer = false) : base(texture, size) {
            _elapsedFrameTime = 0f;
            _currentFrame = 0;
            _lastFrame = -1;
            _currentDirection = Direction.Down;

            PlayerSkin skin = Enum.Parse<PlayerSkin>(texture.Name.Split('/').Last());

            if (isLocalPlayer) {
                VFXNode flagNode = new(
                    TextureHolder.Get($"Item/FlagRed"),
                    new Vector2(size.X / 3 * 2, size.Y / 3 * 2),
                    4,
                    0.1f,
                    1f,
                    true,
                    true
                ) {
                    Position = new Vector2(16, -24),
                };
                AttachChild(flagNode);
            }
        }

        public void Die() {
            _lastDie = DateTime.Now;
            SetDirection(Direction.Down);
        }

        public void SetDirection(Direction dir) {
            if (_currentDirection != dir) {
                _currentDirection = dir;
                _currentFrame = 0;
                _elapsedFrameTime = 0f;
                _lastFrame = -1;
            }
        }

        public void TeleportTo(Vector2 target, Direction direction) {
            base.TeleportTo(target);
            SetDirection(direction);
        }

        public void MoveTo(Vector2 target, Direction direction, float durationSeconds = 0.2f) {
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
            float alpha = 1f;
            if (_lastDie != DateTime.MinValue) {
                double elapsedTime = (DateTime.Now - _lastDie).TotalMilliseconds;
                if (elapsedTime > 2000) {
                    _lastDie = DateTime.MinValue;
                } else {
                    if (Math.Sin(elapsedTime / 100) > 0) {
                        alpha = 0.5f;
                    }
                }
            }

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
                Color.White * alpha,
                rotation,
                _origin,
                scale * textureScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
