using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Client.Component;
using Client.Animation;

namespace Client {
    public class ToastManager {
        private static ToastManager _instance;
        public static ToastManager Instance => _instance ??= new ToastManager();

        private List<ToastNotification> _activeToasts = new List<ToastNotification>();
        private object _lock = new();

        private ToastManager() { }

        public void ShowToast(string message) {
            var toast = new ToastNotification(message);
            lock (_lock) {
                _activeToasts.Add(toast);
            }
        }

        public void Update(GameTime gameTime) {
            lock (_lock) {
                for (int i = _activeToasts.Count - 1; i >= 0; i--) {
                    _activeToasts[i].Update(gameTime);

                    if (_activeToasts[i].IsFinished) {
                        _activeToasts.RemoveAt(i);
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch) {
            lock (_lock) {
                foreach (var toast in _activeToasts) {
                    toast.Draw(spriteBatch);
                }
            }
        }
    }

    public class ToastNotification {
        private TextBox _textBox;
        private IUIAnimation _slideInAnimation;
        private IUIAnimation _stayAnimation;
        private IUIAnimation _slideOutAnimation;

        private enum ToastState {
            SlideIn,
            Staying,
            SlideOut,
            Finished
        }
        private ToastState _currentState = ToastState.SlideIn;

        public bool IsFinished => _currentState == ToastState.Finished;

        public ToastNotification(string message) {
            _textBox = new TextBox() {
                Width = 500,
                Height = 80,
                Text = message,
                TextColor = Color.Black,
                Gravity = Gravity.Center,
                IsReadOnly = true
            };
            _textBox.Position = new Vector2((GameScreen.ScreenSize.X - _textBox.Size.X) / 2, 0);
            CreateAnimations();
        }

        private void CreateAnimations() {
            _slideInAnimation = new TranslateAnimation(
                new Vector2(_textBox.Position.X, -_textBox.Height),
                new Vector2(_textBox.Position.X, 20),
                0.5f,
                Easing.QuadraticEaseOut
            );
            _stayAnimation = new DelayAnimation(2f);
            _slideOutAnimation = new TranslateAnimation(
                new Vector2(_textBox.Position.X, 20),
                new Vector2(_textBox.Position.X, -_textBox.Height),
                0.5f,
                Easing.QuadraticEaseIn
            );
        }

        public void Update(GameTime gameTime) {
            if (_currentState != ToastState.Finished) {
                _textBox.Update(gameTime);
            }

            switch (_currentState) {
                case ToastState.SlideIn:
                    _slideInAnimation.Update(_textBox, gameTime);
                    if (_slideInAnimation.IsFinished) {
                        _currentState = ToastState.Staying;
                    }
                    break;

                case ToastState.Staying:
                    _stayAnimation.Update(_textBox, gameTime);
                    if (_stayAnimation.IsFinished) {
                        _currentState = ToastState.SlideOut;
                    }
                    break;

                case ToastState.SlideOut:
                    _slideOutAnimation.Update(_textBox, gameTime);
                    if (_slideOutAnimation.IsFinished) {
                        _currentState = ToastState.Finished;
                    }
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch) {
            if (_currentState != ToastState.Finished) {
                _textBox.Draw(spriteBatch);
            }
        }
    }

    public class TranslateAnimation : IUIAnimation {
        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private float _duration;
        private float _elapsedTime;
        private Func<float, float> _easingFunction;

        public bool IsFinished => _elapsedTime >= _duration;

        public TranslateAnimation(Vector2 startPosition, Vector2 endPosition, float duration, Func<float, float> easingFunction) {
            _startPosition = startPosition;
            _endPosition = endPosition;
            _duration = duration;
            _easingFunction = easingFunction;
        }

        public void OnStart(IComponent component) { }

        public void Update(IComponent component, GameTime gameTime) {
            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            float t = Math.Clamp(_elapsedTime / _duration, 0f, 1f);
            float easedT = _easingFunction(t);

            component.Position = Vector2.Lerp(_startPosition, _endPosition, easedT);
        }
    }

    public class DelayAnimation : IUIAnimation {
        private float _duration;
        private float _elapsedTime;

        public bool IsFinished => _elapsedTime >= _duration;

        public DelayAnimation(float duration) {
            _duration = duration;
        }

        public void OnStart(IComponent component) { }

        public void Update(IComponent component, GameTime gameTime) {
            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}