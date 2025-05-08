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
            var layout = new LinearLayout() {
                LayoutOrientation = LinearLayout.Orientation.Vertical,
                Width = ScreenSize.X,
                Height = ScreenSize.Y,
            };

            var Text = new TextComponent() {
                Text = "Welcome to the Game",
                FontSize = 2.0f,
                TextAlignment = ContentAlignment.MiddleCenter,
                IsShadowEnabled = true,
                ShadowOffset = new Vector2(2, 2),
                ShadowColor = new Color(0, 0, 0, 128),
            };

            layout.AddComponent(Text);
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

            // if (!usernameText.IsFocused && currentUsername != usernameText.Text) {
            //     currentUsername = usernameText.Text;
            //     NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.SetUsername, new Dictionary<string, string> {
            //         { "username", currentUsername }
            //     }));
            // }
        }
    }
}