using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;

namespace Client {
    public class EndGameScreen : GameScreen {
        private Button mainMenuButton;
        private Button rematchButton;
        private LinearLayout layout;

        public override void Initialize() {
            layout = new LinearLayout(LinearLayout.Orientation.Vertical, spacing: 30) {
                Position = new Vector2(50, 50),
                Size = new Vector2(400, 500),
                Padding = 30,
            };
            layout.Center(new Rectangle(0, 0, Client.Instance.GraphicsDevice.Viewport.Width, Client.Instance.GraphicsDevice.Viewport.Height));

            rematchButton = new Button() {
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 200),
                OnClick = Rematch,
                Text = "Rematch",
            };

            mainMenuButton = new Button() {
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 200),
                OnClick = NavigateToMainMenu,
                Text = "Main Menu",
            };

            layout.AddComponent(rematchButton);
            layout.AddComponent(mainMenuButton);
            uiManager.AddComponent(layout, 0);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            base.Draw(gameTime, spriteBatch);

            Dictionary<string, string> gameResults = GetGameResults();

        }

        private Dictionary<string, string> GetGameResults() {
            // This method should return the game results. For now, we return an empty dictionary.
            return new Dictionary<string, string>();
        }

        private void NavigateToMainMenu() {
            ScreenManager.Instance.NavigateTo(ScreenName.MainMenu);
        }

        private void Rematch() {
            ScreenManager.Instance.NavigateTo(ScreenName.LobbyScreen);
        }

        public override void HandleResponse(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.RoomJoined: {
                        ScreenManager.Instance.NavigateTo(ScreenName.LobbyScreen);
                    }
                    break;
                case ServerMessageType.Error: {
                        Console.WriteLine($"Error joining room: {message.Data["message"]}");
                    }
                    break;
            }
        }
    }
}