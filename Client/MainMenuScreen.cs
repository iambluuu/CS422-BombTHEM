using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;

namespace Client {
    public class MainMenuScreen : GameScreen {
        string _currentName = string.Empty;
        TextBox _usernameBox;

        public override void Initialize() {
            var layout = new LinearLayout() {
                LayoutOrientation = Orientation.Vertical,
                Width = ScreenSize.X,
                Height = ScreenSize.Y,
                Gravity = Gravity.Center,
            };

            var mainBox = new ContainerBox() {
                LayoutOrientation = Orientation.Vertical,
                HeightMode = SizeMode.WrapContent,
                Width = 500,
                Spacing = 20,
            };

            var title = new TextView() {
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Text = "Bomb THEM",
                TextSize = 2f,
                TextColor = Color.White,
                Gravity = Gravity.Center,
                PaddingBottom = 20,
            };

            _usernameBox = new TextBox() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Player",
                PlaceholderText = "Enter username",
                TextColor = Color.Black,
                Gravity = Gravity.Center,
                MaxLength = 10,
                IsReadOnly = false,
            };

            var createButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Create Game",
                OnClick = CreateGame,
            };

            var joinButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Join Game",
                OnClick = JoinGame,
            };

            var exitButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Exit",
                OnClick = ExitGame,
            };

            layout.AddComponent(mainBox);
            mainBox.AddComponent(title);
            mainBox.AddComponent(_usernameBox);
            mainBox.AddComponent(createButton);
            mainBox.AddComponent(joinButton);
            mainBox.AddComponent(exitButton);

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

            if (!_usernameBox.IsFocused && _currentName != _usernameBox.Text) {
                _currentName = _usernameBox.Text;
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.SetUsername, new() {
                    { "username", _currentName }
                }));
            }
        }
    }
}
