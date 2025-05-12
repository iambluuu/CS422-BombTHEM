using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;
using Microsoft.VisualBasic.Devices;

namespace Client {
    public class MainMenuScreen : GameScreen {
        private LinearLayout _connectLayout, _mainLayout;
        private TextBox _addressBox, _portBox;
        private Button _connectButton;
        private string _currentName = string.Empty;
        private TextBox _usernameBox;

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
                Width = 600,
                Spacing = 20,
            };
            layout.AddComponent(mainBox);

            var title = new TextView() {
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Text = "Bomb THEM",
                TextSize = 2f,
                TextColor = Color.White,
                Gravity = Gravity.Center,
                PaddingBottom = 20,
            };
            mainBox.AddComponent(title);

            _connectLayout = new() {
                LayoutOrientation = Orientation.Vertical,
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Spacing = 20,
            };
            mainBox.AddComponent(_connectLayout);

            var addressLayout = new LinearLayout() {
                LayoutOrientation = Orientation.Horizontal,
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Spacing = 20,
            };
            _connectLayout.AddComponent(addressLayout);

            _addressBox = new TextBox() {
                HeightMode = SizeMode.MatchParent,
                Weight = 1,
                AllowedCharacters = CharacterSet.Alphanumeric | CharacterSet.Dot,
                Text = "localhost",
                PlaceholderText = "Address",
                TextColor = Color.Black,
                Gravity = Gravity.CenterLeft,
                IsReadOnly = false,
                TruncateAt = TruncateAt.Middle,
                PaddingLeft = 30,
                PaddingRight = 30,
            };
            addressLayout.AddComponent(_addressBox);

            _portBox = new TextBox() {
                HeightMode = SizeMode.MatchParent,
                Width = 160,
                AllowedCharacters = CharacterSet.Numeric,
                Text = "5000",
                MaxLength = 5,
                PlaceholderText = "Port",
                TextColor = Color.Black,
                Gravity = Gravity.Center,
                IsReadOnly = false,
            };
            addressLayout.AddComponent(_portBox);

            _connectButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Connect",
                OnClick = Connect,
            };
            _connectLayout.AddComponent(_connectButton);

            _mainLayout = new() {
                LayoutOrientation = Orientation.Vertical,
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Spacing = 20,
                IsVisible = false,
            };
            mainBox.AddComponent(_mainLayout);

            _usernameBox = new TextBox() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                AllowedCharacters = CharacterSet.Alphanumeric | CharacterSet.Underscore,
                Text = "Player",
                PlaceholderText = "Enter username",
                TextColor = Color.Black,
                Gravity = Gravity.Center,
                MaxLength = 10,
                IsReadOnly = false,
            };
            _mainLayout.AddComponent(_usernameBox);

            var createButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Create Game",
                OnClick = CreateGame,
            };
            _mainLayout.AddComponent(createButton);

            var joinButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Join Game",
                OnClick = JoinGame,
            };
            _mainLayout.AddComponent(joinButton);

            var exitButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Exit",
                OnClick = ExitGame,
            };
            mainBox.AddComponent(exitButton);

            uiManager.AddComponent(layout, 0);
        }

        public override void Activate() {
            base.Activate();

            if (NetworkManager.Instance.IsConnected) {
                _connectLayout.IsVisible = false;
                _connectButton.IsEnabled = false;
                _mainLayout.IsVisible = true;
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetUsername));
            } else {
                _connectLayout.IsVisible = true;
                _connectButton.IsEnabled = true;
                _mainLayout.IsVisible = false;
                _currentName = string.Empty;
            }
        }

        private void Connect() {
            _connectButton.IsEnabled = false;
            NetworkManager.Instance.Connect(_addressBox.Text, int.Parse(_portBox.Text));
        }

        private void CreateGame() {
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.CreateRoom));
        }

        public override void HandleResponse(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.Connected: {
                        NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetClientId));
                    }
                    break;
                case ServerMessageType.NotConnected: {
                        _connectLayout.IsVisible = true;
                        _connectButton.IsEnabled = true;
                        _mainLayout.IsVisible = false;
                        _currentName = string.Empty;
                    }
                    break;
                case ServerMessageType.ClientId: {
                        _connectLayout.IsVisible = false;
                        _connectButton.IsEnabled = false;
                        _mainLayout.IsVisible = true;
                        NetworkManager.Instance.ClientId = int.Parse(message.Data["clientId"]);
                    }
                    break;
                case ServerMessageType.UsernameSet: {
                        _usernameBox.Text = message.Data["username"];
                        _currentName = _usernameBox.Text;
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

            if (NetworkManager.Instance.IsConnected && !_usernameBox.IsFocused && _currentName != _usernameBox.Text) {
                _currentName = _usernameBox.Text;
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.SetUsername, new() {
                    { "username", _currentName }
                }));
            }
        }
    }
}
