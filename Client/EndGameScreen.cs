using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;
using Client.Animation;

namespace Client {
    public class EndGameScreen : GameScreen {
        private LinearLayout layout;
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

        private void NavigateToMainMenu() {
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.LeaveRoom));
            ScreenManager.Instance.NavigateToRoot();
        }

        private void Rematch() {
            // Maybe send a rematch request to the server here?
            // ScreenManager.Instance.NavigateBackTo(ScreenName.LobbyScreen);
        }

        public override void HandleResponse(NetworkMessage message) {
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

                        Console.WriteLine($"Game results: {string.Join(", ", _gameResults)}");
                        OnResultsArrived();
                    }
                    break;
                case ServerMessageType.Error: {
                        Console.WriteLine($"Error joining room: {message.Data["message"]}");
                    }
                    break;
            }
        }

        private void OnResultsArrived() {
            hasResultArrived = true;
            Array.Sort(_gameResults, (x, y) => y.Item3.CompareTo(x.Item3)); // Sort by score descending
            var winner = _gameResults[0];

            layout = new LinearLayout() {
                LayoutOrientation = LinearLayout.Orientation.Vertical,
                Position = new Vector2(50, 50),
                Size = new Vector2(400, 600),
            };

            var screenTitle = new TextBox(hasBackground: false) {
                Text = "Finished!",
                FontSize = 60,
                TextColor = Color.White,
                Padding = new Padding(0),
                TextAlignment = ContentAlignment.MiddleCenter,
            };

            var winnerImage = new ImageComponent(TextureHolder.Get($"Texture/Character/{(PlayerSkin)_skinMapping[winner.Item1]}", new Rectangle(0, 0, 16, 13)), ScaleMode.Fit);
            var winnerName = new TextBox(hasBackground: false) {
                Text = $"Winner: {winner.Item2}",
                FontSize = 40,
                TextColor = Color.White,
                Padding = new Padding(0),
                TextAlignment = ContentAlignment.MiddleCenter,
            };
            var winnerScore = new TextBox(hasBackground: false) {
                Text = $"Score: {winner.Item3}",
                FontSize = 40,
                Padding = new Padding(0),
                TextColor = Color.White,
                TextAlignment = ContentAlignment.TopCenter,
            };

            var buttonLayout = new LinearLayout() {
                LayoutOrientation = LinearLayout.Orientation.Vertical,
                Size = new Vector2(400, 100),
            };
            var rematchButton = new Button() {
                Size = new Vector2(100, 200),
                OnClick = Rematch,
                Text = "Rematch",
            };

            var mainMenuButton = new Button() {
                Size = new Vector2(100, 200),
                OnClick = NavigateToMainMenu,
                Text = "Main Menu",
            };

            layout.AddComponent(screenTitle, 2);
            layout.AddComponent(winnerImage, 2);
            layout.AddComponent(winnerName, 1);
            layout.AddComponent(winnerScore, 1);
            buttonLayout.AddComponent(rematchButton);
            buttonLayout.AddComponent(mainMenuButton);
            layout.AddComponent(buttonLayout, 3);
            layout.Center(new Rectangle(0, 0, Client.Instance.GraphicsDevice.Viewport.Width, Client.Instance.GraphicsDevice.Viewport.Height));
            uiManager.AddComponent(layout, 0);

            layout.AddAnimation(new MoveFromAnimation(layout.Position + new Vector2(0, StartingPositionOffset), 2f, Easing.QuadraticEaseInOut));
        }
    }
}