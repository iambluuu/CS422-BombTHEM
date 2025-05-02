using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using System;

namespace Client {
    public class JoinGameScreen : GameScreen {
        public int ServerPort { get; set; } = 0;
        public bool IsConnecting { get; set; } = false;
        public bool IsConnected { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;

        private TextBox roomCodeBox = null!;

        public override void Initialize() {
            base.Initialize();

            var layout = new LinearLayout(LinearLayout.Orientation.Vertical, new List<IComponent>(), spacing: 30) {
                Position = new Vector2(50, 50),
                Size = new Vector2(300, 400),
                Padding = 25,
            };
            roomCodeBox = new TextBox() {
                PlaceholderText = "Enter Room Code",
                Font = content.Load<SpriteFont>("Font/NormalFont"),
                MaxLength = 6,
                FontSize = 100,
                TextAlignment = ContentAlignment.MiddleCenter,
                IsUppercase = true,
            };

            var connectButton = new Button(position: new Vector2(0, 0), size: new Vector2(100, 200), onClick: Connect) {
                Text = "Join",
                Font = content.Load<SpriteFont>("Font/NormalFont"),
            };

            layout.Center(new Rectangle(0, 0, MainGame.Instance.GraphicsDevice.Viewport.Width, MainGame.Instance.GraphicsDevice.Viewport.Height));
            layout.AddComponent(roomCodeBox);
            layout.AddComponent(connectButton);
            uiManager.AddComponent(layout);
        }

        public void Connect() {
            if (IsConnecting) return;

            string roomCode = roomCodeBox.Text.Trim();
            if (!ValidateRoomCode(roomCode)) {
                ErrorMessage = $"Invalid room code: {roomCode}. Please try again.";
                Console.WriteLine(ErrorMessage);
                return;
            }

            Console.WriteLine($"Attempting to connect to room: {roomCode} on port {ServerPort}");
            IsConnecting = true;
            // Simulate connection logic here
            // On success:
            IsConnected = true;
            // On failure:
            ErrorMessage = "Failed to connect to server.";
            IsConnecting = false;
        }

        private bool ValidateRoomCode(string roomCode) {
            // Add your validation logic here (e.g., regex, length check, etc.)
            return !string.IsNullOrEmpty(roomCode) && roomCode.Length == 6;
        }

        public void Disconnect() {
            if (!IsConnected) return;

            IsConnected = false;
            // Simulate disconnection logic here
        }
    }
}