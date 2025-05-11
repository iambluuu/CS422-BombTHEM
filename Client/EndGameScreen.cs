using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;
using Client.Animation;
using System.Text.RegularExpressions;

namespace Client {
    public class EndGameScreen : GameScreen {
        private (int, string, int)[] _gameResults = Array.Empty<(int, string, int)>();
        private Dictionary<int, int> _skinMapping = [];
        private bool hasResultArrived = false;

        private const float ScreenDarkenOpacity = 0.8f;
        private const float ScreenDarkenDuration = 1f;
        private const int StartingPositionOffset = 1000;

        private float _elapsedTime = 0f;

        public override void Activate() {
            base.Activate();
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetGameResults));
        }

        public override void LoadParameters(Dictionary<string, object> parameters) {
            if (parameters.ContainsKey("skinMapping")) {
                _skinMapping = parameters["skinMapping"] as Dictionary<int, int>;
                if (_skinMapping != null) {

                } else {
                    Console.WriteLine("Skin mapping is null.");
                }
            } else {
                Console.WriteLine("Skin mapping not found in parameters.");
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            if (_elapsedTime < ScreenDarkenDuration) {
                _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            float t = MathHelper.Clamp(_elapsedTime / ScreenDarkenDuration, 0f, 1f);
            float opacity = MathHelper.Lerp(0f, ScreenDarkenOpacity, t);
            var blackTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            blackTexture.SetData(new[] { Color.Black * opacity });
            spriteBatch.Draw(blackTexture, new Rectangle(0, 0, Client.Instance.GraphicsDevice.Viewport.Width, Client.Instance.GraphicsDevice.Viewport.Height), new Color(0, 0, 0, opacity));
            if (!hasResultArrived)
                return;
            base.Draw(gameTime, spriteBatch);
        }

        private void Rematch() {
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.LeaveGame));
            ScreenManager.Instance.NavigateBackTo(ScreenName.LobbyScreen);
        }

        public override void HandleResponse(NetworkMessage message) {
            base.HandleResponse(message);

            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.RoomJoined: {
                        ScreenManager.Instance.NavigateTo(ScreenName.LobbyScreen, isOverlay: false);
                    }
                    break;
                case ServerMessageType.GameResults: {
                        string[] playerIds = message.Data["playerIds"].Split(';');
                        string[] playerNames = message.Data["usernames"].Split(';');
                        int[] playerScores = Array.ConvertAll(message.Data["scores"].Split(';'), int.Parse);
                        _gameResults = new (int, string, int)[playerIds.Length];
                        for (int i = 0; i < playerIds.Length; i++) {
                            _gameResults[i] = (int.Parse(playerIds[i]), playerNames[i], playerScores[i]);
                        }

                        OnResultsArrived();
                    }
                    break;
                case ServerMessageType.Error: {
                        Console.WriteLine($"Error: {message.Data["message"]}");
                    }
                    break;
            }
        }

        private void OnResultsArrived() {
            hasResultArrived = true;
            Array.Sort(_gameResults, (x, y) => y.Item3.CompareTo(x.Item3));
            var winner = _gameResults[0];

            var layout = new LinearLayout() {
                LayoutOrientation = Orientation.Vertical,
                Width = ScreenSize.X,
                Height = ScreenSize.Y,
                Gravity = Gravity.Center,
            };
            layout.AddAnimation(new MoveFromAnimation(layout.Position + new Vector2(0, StartingPositionOffset), 2f, Easing.QuadraticEaseInOut));

            var mainBox = new ContainerBox() {
                LayoutOrientation = Orientation.Vertical,
                Width = 500,
                HeightMode = SizeMode.WrapContent,
                Spacing = 10,
            };
            layout.AddComponent(mainBox);

            var screenTitle = new TextView() {
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Text = "Winner",
                TextSize = 2,
                TextColor = Color.White,
                Gravity = Gravity.CenterHorizontal,
            };
            mainBox.AddComponent(screenTitle);

            var winnerImage = new ImageView() {
                WidthMode = SizeMode.MatchParent,
                Height = 100,
                Texture = TextureHolder.Get($"Character/{(PlayerSkin)_skinMapping[winner.Item1]}", new Rectangle(0, 0, 16, 13)),
                ScaleType = ScaleType.FitCenter,
                Gravity = Gravity.CenterHorizontal,
            };
            mainBox.AddComponent(winnerImage);

            var winnerName = new TextView() {
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Text = $"{winner.Item2}",
                TextColor = Color.White,
                Gravity = Gravity.CenterHorizontal,
            };
            mainBox.AddComponent(winnerName);

            var winnerScore = new TextView() {
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Text = $"Score: {winner.Item3}",
                TextColor = Color.White,
                Gravity = Gravity.CenterHorizontal,
            };
            mainBox.AddComponent(winnerScore);

            var boxesLayout = new LinearLayout() {
                LayoutOrientation = Orientation.Vertical,
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Spacing = 10,
                PaddingTop = 20,
            };
            mainBox.AddComponent(boxesLayout);

            var rematchButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Back to Lobby",
                TextColor = Color.White,
                OnClick = Rematch,
                PaddingTop = 10,
            };
            boxesLayout.AddComponent(rematchButton);

            uiManager.AddComponent(layout, 0);
        }
    }
}