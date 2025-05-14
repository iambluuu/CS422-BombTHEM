using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;

namespace Client {
    public class JoinGameScreen : GameScreen {
        private TextBox _roomIdBox;

        public override void Initialize() {
            base.Initialize();

            var layout = new LinearLayout() {
                LayoutOrientation = Orientation.Vertical,
                Width = ScreenSize.X,
                Height = ScreenSize.Y,
                Gravity = Gravity.Center,
            };
            uiManager.AddComponent(layout);

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
                Text = "Join Game",
                TextSize = 2f,
                TextColor = Color.White,
                Gravity = Gravity.Center,
                PaddingBottom = 20,
            };
            mainBox.AddComponent(title);

            _roomIdBox = new TextBox() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                AllowedCharacters = CharacterSet.Alpha,
                PlaceholderText = "Enter room ID",
                TextColor = Color.Black,
                Gravity = Gravity.Center,
                MaxLength = 6,
                IsUppercase = true,
            };
            mainBox.AddComponent(_roomIdBox);

            var joinButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Join",
                OnClick = Connect,
            };
            mainBox.AddComponent(joinButton);

            var backButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Back",
                OnClick = ScreenManager.Instance.NavigateBack,
            };
            mainBox.AddComponent(backButton);
        }

        private void Connect() {
            string roomId = _roomIdBox.Text.ToUpperInvariant();
            if (ValidateRoomCode(roomId)) {
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.JoinRoom, new() {
                    {"roomId", roomId }
                }));
            } else {
                ToastManager.Instance.ShowToast("Code length must be 6");
            }
        }

        private bool ValidateRoomCode(string roomCode) {
            return !string.IsNullOrEmpty(roomCode) && roomCode.Length == 6;
        }

        public override void HandleResponse(NetworkMessage message) {
            base.HandleResponse(message);

            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.RoomJoined: {
                        ScreenManager.Instance.StartLoading();
                        ScreenManager.Instance.NavigateTo(ScreenName.LobbyScreen);
                    }
                    break;
                case ServerMessageType.Error: {
                        ToastManager.Instance.ShowToast(message.Data["message"]);
                    }
                    break;
            }
        }
    }
}