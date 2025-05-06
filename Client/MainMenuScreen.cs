using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;
using System;

namespace Client {
    public class MainMenuScreen : GameScreen {
        string currentUsername = string.Empty;
        TextBox usernameText;

        public override void Initialize() {
            // Initialize your main menu components here
            var layout = new LinearLayout(LinearLayout.Orientation.Vertical, spacing: 30) {
                Position = new Vector2(50, 50),
                Size = new Vector2(400, 500),
                Padding = 30,
            };

            layout.Center(new Rectangle(0, 0, Client.Instance.GraphicsDevice.Viewport.Width, Client.Instance.GraphicsDevice.Viewport.Height));
            usernameText = new TextBox() {
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 200),
                Text = "Player",
                TextAlignment = ContentAlignment.MiddleCenter,
            };

            var createGameButton = new Button() {
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 200),
                OnClick = CreateGame,
                Text = "Create Game",
            };
            var exitButton = new Button() {
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 200),
                OnClick = ExitGame,
                Text = "Exit",
            };
            var joinGameButton = new Button() {
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 200),
                OnClick = JoinGame,
                Text = "Join Game",
            };

            layout.AddComponent(usernameText);
            layout.AddComponent(createGameButton);
            layout.AddComponent(joinGameButton);
            layout.AddComponent(exitButton);
            uiManager.AddComponent(layout, 0);

            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetClientId));
        }

        private void CreateGame() {
            if (NetworkManager.Instance.ClientId == -1) {
                Console.WriteLine("Client ID not set, cannot create room");
                return;
            }

            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.CreateRoom));
        }

        public override void HandleResponse(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.ClientId: {
                        NetworkManager.Instance.ClientId = int.Parse(message.Data["clientId"]);
                    }
                    break;
                case ServerMessageType.RoomCreated: {
                        ScreenManager.Instance.NavigateTo(ScreenName.LobbyScreen);
                    }
                    break;
                case ServerMessageType.Error: {
                        Console.WriteLine($"Error: {message.Data["message"]}");
                    }
                    break;
            }

        }

        private void ExitGame() {
            UnloadContent();
            Client.Instance.Exit();
            Environment.Exit(0);
        }

        private void JoinGame() {
            ScreenManager.Instance.NavigateTo(ScreenName.JoinGameScreen);
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (!usernameText.IsFocused && currentUsername != usernameText.Text) {
                currentUsername = usernameText.Text;
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.SetUsername, new Dictionary<string, string> {
                    { "username", currentUsername }
                }));
            }
        }
    }
}