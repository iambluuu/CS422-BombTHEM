using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Scene {
    public class MoveableSpriteNode : SpriteNode {
        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private float _moveDuration;
        private float _elapsedTime;
        private bool _moving;

        public bool Moving => _moving;

        public MoveableSpriteNode(Texture2D texture) : base(texture) {
            _moving = false;
        }

        public MoveableSpriteNode(Texture2D texture, Vector2 size) : base(texture, size) {
            _moving = false;
        }

        public void TeleportTo(Vector2 target) {
            Position = target;
            _moving = false;
        }

        public void MoveTo(Vector2 target, float durationSeconds) {
            _startPosition = Position;
            _targetPosition = target;
            _moveDuration = durationSeconds;
            _elapsedTime = 0f;
            _moving = true;
        }

        protected override void UpdateCurrent(GameTime gameTime) {
            if (_moving) {
                _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                float t;
                if (_moveDuration > 0f) {
                    t = _elapsedTime / _moveDuration;
                } else {
                    t = 1f;
                }

                if (t >= 1f) {
                    t = 1f;
                    _moving = false;
                }

                Position = Vector2.Lerp(_startPosition, _targetPosition, t);
            }
        }
    }
}
