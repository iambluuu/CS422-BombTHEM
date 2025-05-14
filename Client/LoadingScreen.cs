using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Client.Component;
using System;
using Shared;
using Client.Animation;

namespace Client {
    class LoadingScreen : GameScreen {
        private enum DoorState {
            None,
            Closing,
            Waiting,
            Opening,
        }

        private LinearLayout _leftDoor;
        private LinearLayout _rightDoor;
        private float _doorMoveTime = 0.5f;
        private float _currentDoorTime = 0f;
        private DoorState _doorState = DoorState.None;
        private bool _requestOpen = false;

        public bool IsLoading => _doorState == DoorState.Closing || _doorState == DoorState.Waiting;

        public override void Initialize() {
            _leftDoor = new() {
                LayoutOrientation = Orientation.Vertical,
                Width = ScreenSize.X / 2,
                Height = ScreenSize.Y,
                Position = new Vector2(-ScreenSize.X / 2, 0),
                BackgroundColor = new Color(240, 103, 51),
                Gravity = Gravity.CenterRight,
            };
            uiManager.AddComponent(_leftDoor);

            _rightDoor = new() {
                LayoutOrientation = Orientation.Vertical,
                Width = ScreenSize.X / 2,
                Height = ScreenSize.Y,
                Position = new Vector2(ScreenSize.X, 0),
                BackgroundColor = new Color(240, 103, 51),
                Gravity = Gravity.CenterLeft,
            };
            uiManager.AddComponent(_rightDoor);

            var leftDoorText = new TextView() {
                WidthMode = SizeMode.WrapContent,
                HeightMode = SizeMode.WrapContent,
                Text = "Bomb",
                TextSize = 4.0f,
                PaddingRight = 20,
            };
            _leftDoor.AddComponent(leftDoorText);

            var rightDoorText = new TextView() {
                WidthMode = SizeMode.WrapContent,
                HeightMode = SizeMode.WrapContent,
                Text = "THEM",
                TextSize = 4.0f,
                PaddingLeft = 20,
            };
            _rightDoor.AddComponent(rightDoorText);
        }

        public void RequestClose() {
            _currentDoorTime = 0f;
            _doorState = DoorState.Closing;
        }

        public void RequestOpen() {
            _requestOpen = true;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            _currentDoorTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            float progress = Math.Min(_currentDoorTime / _doorMoveTime, 1f);

            if (_doorState == DoorState.Closing) {
                _leftDoor.X = MathHelper.Lerp(-ScreenSize.X / 2, 0, Easing.CubicEaseIn(progress));
                _rightDoor.X = MathHelper.Lerp(ScreenSize.X, ScreenSize.X / 2, Easing.CubicEaseIn(progress));
            } else if (_doorState == DoorState.Opening) {
                _leftDoor.X = MathHelper.Lerp(0, -ScreenSize.X / 2, Easing.CubicEaseIn(progress));
                _rightDoor.X = MathHelper.Lerp(ScreenSize.X / 2, ScreenSize.X, Easing.CubicEaseIn(progress));
            }

            if (_currentDoorTime >= _doorMoveTime) {
                if (_doorState == DoorState.Closing) {
                    _doorState = DoorState.Waiting;
                    _currentDoorTime = 0f;
                } else if (_doorState == DoorState.Waiting) {
                    if (_requestOpen) {
                        _doorState = DoorState.Opening;
                        _requestOpen = false;
                        _currentDoorTime = 0f;
                    }
                } else if (_doorState == DoorState.Opening) {
                    _doorState = DoorState.None;
                }
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            if (_doorState != DoorState.None) {
                base.Draw(gameTime, spriteBatch);
            }
        }
    }
}