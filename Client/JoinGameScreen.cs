using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;

namespace Client {
    public class JoinGameScreen : GameScreen {
        private TextBox roomIdTextBox = null!;

        public override void Initialize() {
            base.Initialize();

            var layout = new LinearLayout(LinearLayout.Orientation.Vertical, new List<IComponent>(), spacing: 30) {
                Position = new Vector2(50, 50),
                Size = new Vector2(300, 400),
                Padding = 25,
            };
            roomIdTextBox = new TextBox() {
                PlaceholderText = "Enter Room Code",
                MaxLength = 6,
                TextAlignment = ContentAlignment.MiddleCenter,
                IsUppercase = true,
            };

            var connectButton = new Button() {
                Text = "Join",
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 200),
                OnClick = Connect,
            };

            var backButton = new Button() {
                Text = "Back",
                Position = new Vector2(0, 0),
                Size = new Vector2(100, 200),
                OnClick = () => ScreenManager.Instance.NavigateBack(),
            };

            layout.Center(new Rectangle(0, 0, Client.Instance.GraphicsDevice.Viewport.Width, Client.Instance.GraphicsDevice.Viewport.Height));
            layout.AddComponent(roomIdTextBox);
            layout.AddComponent(connectButton);
            layout.AddComponent(backButton);
            uiManager.AddComponent(layout);
        }

        private void Connect() {
            string roomId = roomIdTextBox.Text.ToUpperInvariant();
            if (ValidateRoomCode(roomId)) {
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.JoinRoom, new() {
                    {"roomId", roomId }
                }));
            } else {
                Console.WriteLine("Invalid room code. Please enter a 6-character code");
            }
        }

        private bool ValidateRoomCode(string roomCode) {
            return !string.IsNullOrEmpty(roomCode) && roomCode.Length == 6;
        }

        public override void HandleResponse(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.RoomJoined: {
                        ScreenManager.Instance.NavigateTo(ScreenName.LobbyScreen);
                    }
                    break;
                case ServerMessageType.Error: {
                        Console.WriteLine($"Error: {message.Data["message"]}");
                    }
                    break;
            }
        }
    }
}