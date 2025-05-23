using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

using Client.Animation;
using Client.Component;
using Client.ContentHolder;

namespace Client.Screen {
    class LoadingScreen : GameScreen {
        private enum DoorState {
            None,
            Closing,
            Waiting,
            Opening,
        }

        private LinearLayout _leftDoorText;
        private ImageView _leftDoor;
        private LinearLayout _rightDoorText;
        private ImageView _rightDoor;
        private readonly float _doorMoveTime = 0.5f;
        private float _currentDoorTime = 0f;
        private DoorState _doorState = DoorState.None;
        private bool _requestOpen = false;

        public bool IsLoading => _doorState == DoorState.Closing || _doorState == DoorState.Waiting;

        public override void Initialize() {
            _leftDoor = new ImageView() {
                Width = ScreenSize.X / 2,
                Height = ScreenSize.Y,
                Texture = TextureHolder.Get("Theme/door_left"),
                Position = new Vector2(-ScreenSize.X / 2, 0),
                ScaleType = ScaleType.FitCenter,
            };
            uiManager.AddComponent(_leftDoor);

            _rightDoor = new ImageView() {
                Width = ScreenSize.X / 2,
                Height = ScreenSize.Y,
                Texture = TextureHolder.Get("Theme/door_right"),
                Position = new Vector2(ScreenSize.X, 0),
                ScaleType = ScaleType.FitCenter,
            };
            uiManager.AddComponent(_rightDoor);

            _leftDoorText = new() {
                LayoutOrientation = Orientation.Vertical,
                Width = ScreenSize.X / 2,
                Height = ScreenSize.Y - 100,
                Position = new Vector2(-ScreenSize.X / 2, 0),
                Gravity = Gravity.CenterRight,
            };
            uiManager.AddComponent(_leftDoorText);

            _rightDoorText = new() {
                LayoutOrientation = Orientation.Vertical,
                Width = ScreenSize.X / 2,
                Height = ScreenSize.Y - 100,
                Position = new Vector2(ScreenSize.X, 0),
                Gravity = Gravity.CenterLeft,
            };
            uiManager.AddComponent(_rightDoorText);

            var leftDoorText = new ImageView() {
                WidthMode = SizeMode.MatchParent,
                Height = 130,
                Texture = TextureHolder.Get("Logo", new Rectangle(0, 0, 900, 250)),
                Gravity = Gravity.CenterRight,
                ScaleType = ScaleType.FitEnd,
                PaddingRight = 70,
            };
            _leftDoorText.AddComponent(leftDoorText);

            var rightDoorText = new ImageView() {
                WidthMode = SizeMode.MatchParent,
                Height = 130,
                Texture = TextureHolder.Get("Logo", new Rectangle(900, 0, 900, 250)),
                Gravity = Gravity.CenterLeft,
                ScaleType = ScaleType.FitStart,
                PaddingLeft = 70,
            };
            _rightDoorText.AddComponent(rightDoorText);
        }

        public void RequestClose() {
            _currentDoorTime = 0f;
            _doorState = DoorState.Closing;
        }

        public void RequestOpen() {
            if (_doorState != DoorState.None && _doorState != DoorState.Opening) {
                _requestOpen = true;
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            _currentDoorTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            float progress = Math.Min(_currentDoorTime / _doorMoveTime, 1f);

            if (_doorState == DoorState.Closing) {
                if (progress < 0.8f) {
                    progress /= 0.8f;
                    _leftDoor.X = _leftDoorText.X = MathHelper.Lerp(-ScreenSize.X / 2, 0, Easing.CubicEaseIn(progress));
                    _rightDoor.X = _rightDoorText.X = MathHelper.Lerp(ScreenSize.X, ScreenSize.X / 2, Easing.CubicEaseIn(progress));
                } else {
                    float bounceNormalProgress = (progress - 0.8f) / 0.2f;

                    float leftDoorClosedX = 0;
                    float rightDoorClosedX = ScreenSize.X / 2;

                    float bounce1_amplitude = ScreenSize.X * 0.03f;
                    float bounce2_amplitude = ScreenSize.X * 0.01f;

                    const float t_phase1_end = 0.4f;
                    const float t_phase2_end = 0.7f;
                    const float t_phase3_end = 0.85f;
                    const float t_phase4_end = 1.0f;

                    float currentLeftX, currentRightX;

                    if (bounceNormalProgress < t_phase1_end) {
                        float phaseProgress = bounceNormalProgress / t_phase1_end;

                        currentLeftX = MathHelper.Lerp(leftDoorClosedX, leftDoorClosedX - bounce1_amplitude, Easing.SineEaseOut(phaseProgress));
                        currentRightX = MathHelper.Lerp(rightDoorClosedX, rightDoorClosedX + bounce1_amplitude, Easing.SineEaseOut(phaseProgress));

                    } else if (bounceNormalProgress < t_phase2_end) {
                        float phaseProgress = (bounceNormalProgress - t_phase1_end) / (t_phase2_end - t_phase1_end);

                        currentLeftX = MathHelper.Lerp(leftDoorClosedX - bounce1_amplitude, leftDoorClosedX, Easing.SineEaseIn(phaseProgress));
                        currentRightX = MathHelper.Lerp(rightDoorClosedX + bounce1_amplitude, rightDoorClosedX, Easing.SineEaseIn(phaseProgress));

                    } else if (bounceNormalProgress < t_phase3_end) {
                        float phaseProgress = (bounceNormalProgress - t_phase2_end) / (t_phase3_end - t_phase2_end);

                        currentLeftX = MathHelper.Lerp(leftDoorClosedX, leftDoorClosedX - bounce2_amplitude, Easing.SineEaseOut(phaseProgress));
                        currentRightX = MathHelper.Lerp(rightDoorClosedX, rightDoorClosedX + bounce2_amplitude, Easing.SineEaseOut(phaseProgress));
                    } else {
                        float phaseProgress = (bounceNormalProgress - t_phase3_end) / (t_phase4_end - t_phase3_end);

                        currentLeftX = MathHelper.Lerp(leftDoorClosedX - bounce2_amplitude, leftDoorClosedX, Easing.SineEaseIn(phaseProgress));
                        currentRightX = MathHelper.Lerp(rightDoorClosedX + bounce2_amplitude, rightDoorClosedX, Easing.SineEaseIn(phaseProgress));
                    }

                    _leftDoor.X = _leftDoorText.X = currentLeftX;
                    _rightDoor.X = _rightDoorText.X = currentRightX;
                }
            } else if (_doorState == DoorState.Opening) {
                _leftDoor.X = _leftDoorText.X = MathHelper.Lerp(0, -ScreenSize.X / 2, Easing.CubicEaseIn(progress));
                _rightDoor.X = _rightDoorText.X = MathHelper.Lerp(ScreenSize.X / 2, ScreenSize.X, Easing.CubicEaseIn(progress));
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