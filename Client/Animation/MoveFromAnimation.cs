using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Shared;
using Client.Component;

namespace Client.Animation {
    public class MoveFromAnimation : IUIAnimation {
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private float _duration;
        private float _elapsedTime;
        private Func<float, float> _easingFunction;
        private bool _isFinished;

        public MoveFromAnimation(Vector2 start, float duration, Func<float, float> easingFunction = null) {
            _startPosition = start;
            _duration = duration;
            _easingFunction = easingFunction ?? Easing.Linear;
            _isFinished = true; // Animation is not started yet
        }

        public void OnStart(IComponent component) {
            _endPosition = component.Position;
            _isFinished = false;
            _elapsedTime = 0f;
        }

        public void Update(IComponent component, GameTime gameTime) {
            if (_isFinished) return;

            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            float t = MathHelper.Clamp(_elapsedTime / _duration, 0f, 1f);
            float easedT = _easingFunction(t);
            component.Position = Vector2.Lerp(_startPosition, _endPosition, easedT);

            if (t >= 1f) {
                _isFinished = true;
            }
        }

        public bool IsFinished => _isFinished;

    }
}