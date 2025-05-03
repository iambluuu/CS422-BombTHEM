using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Client.Component;
using Shared;
using System;

namespace Client {
    public class MainMenuScreen : GameScreen {
        public override void Initialize() {
            // Initialize your main menu components here
            var layout = new LinearLayout(LinearLayout.Orientation.Vertical, new List<IComponent>(), spacing: 30) {
                Position = new Vector2(50, 50),
                Size = new Vector2(400, 500),
                Padding = 30,
            };

            layout.Center(new Rectangle(0, 0, Client.Instance.GraphicsDevice.Viewport.Width, Client.Instance.GraphicsDevice.Viewport.Height));
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

            layout.AddComponent(createGameButton);
            layout.AddComponent(joinGameButton);
            layout.AddComponent(exitButton);
            uiManager.AddComponent(layout, 0);

            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetClientId));
        }

        private string GenerateRoomId() {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string roomId = string.Empty;
            for (int i = 0; i < 6; i++) {
                roomId += chars[Utils.RandomInt(chars.Length)];
            }

            return roomId;
        }

        private void CreateGame() {
            TryCreateGame();
        }

        private bool _waitingForResponse = false;

        private void TryCreateGame() {
            if (_waitingForResponse) {
                return;
            }

            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.CreateRoom, new() {
                { "roomId", GenerateRoomId() }
            }));
            _waitingForResponse = true;
        }

        public override void HandleResponse(NetworkMessage message) {
            _waitingForResponse = false;
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
                        Console.WriteLine($"Error creating room: {message.Data["message"]}");
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

        public override void LoadContent() {
            base.LoadContent();
        }

        public override void UnloadContent() {
            base.UnloadContent();
            NetworkManager.Instance.Disconnect();
        }

        public override void Update(GameTime gameTime) {
            // Handle input and update UI components
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            // Draw the main menu components
            base.Draw(gameTime, spriteBatch);
        }
    }
}