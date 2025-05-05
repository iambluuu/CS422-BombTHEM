using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client {
    public class BombNode : SpriteNode {
        private DateTime _placeTime;
        private bool _flashOn = false;
        private double _lastFlashTime = 0;

        private const double startFlashingMs = 1000;
        private const double maxDurationMs = 2000;
        private const double minInterval = 30;
        private const double maxInterval = 300;

        public BombNode(Texture2D texture, Vector2 size) : base(texture, size) {
            _placeTime = DateTime.Now;
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

            if (!_flashOn) {
                base.DrawCurrent(spriteBatch, transform);
            }
        }
    }
}
