using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Client.Component;
using Shared;

namespace Client {
    public class LobbyScreen : GameScreen {
        private TextBox[] playerTextBoxes;
        private TextBox roomIdTextBox;
        private Button addBotButton;
        private Button leaveButton;
        private Button startButton;
        private LinearLayout mainLayout;
        private LinearLayout leftLayout;
        private LinearLayout rightLayout;

        public override void Initialize() {
            // Create main horizontal layout
            mainLayout = new LinearLayout(LinearLayout.Orientation.Horizontal, spacing: 20) {
                Position = new Vector2(50, 50),
                Size = new Vector2(1000, 600),
                Padding = 20,
            };
            mainLayout.Center(new Rectangle(0, 0, Client.Instance.GraphicsDevice.Viewport.Width, Client.Instance.GraphicsDevice.Viewport.Height));

            leftLayout = new LinearLayout(LinearLayout.Orientation.Vertical, spacing: 15) {
                Position = Vector2.Zero, // Will be set by parent layout
                Size = Vector2.Zero,     // Will be set by parent layout
                Padding = 10,
            };

            // Create right layout for buttons
            rightLayout = new LinearLayout(LinearLayout.Orientation.Vertical, spacing: 15) {
                Position = Vector2.Zero, // Will be set by parent layout
                Size = Vector2.Zero,     // Will be set by parent layout
                Padding = 10,
            };

            // Create player text boxes and status indicators
            playerTextBoxes = new TextBox[4];

            for (int i = 0; i < 4; i++) {
                // Create a horizontal layout for each player row
                var playerRowLayout = new LinearLayout(LinearLayout.Orientation.Horizontal, spacing: 10) {
                    Position = Vector2.Zero, // Will be set by parent layout
                    Size = Vector2.Zero,     // Will be set by parent layout
                    Padding = 5,
                };

                // Player name/ID text box
                playerTextBoxes[i] = new TextBox {
                    Position = Vector2.Zero, // Will be set by parent layout
                    Size = Vector2.Zero,     // Will be set by parent layout
                    IsReadOnly = true,
                    PlaceholderText = $"Player {i + 1} (Waiting...)",
                    FontSize = 50,
                    TextColor = Color.Black,
                    BackgroundColor = Color.White,
                    BorderColor = Color.Black,
                    BorderWidth = 2,
                    Padding = 10,
                    TextAlignment = ContentAlignment.MiddleCenter,
                };

                // Add components to the player row
                playerRowLayout.AddComponent(playerTextBoxes[i]);

                // Add the row to the left layout
                leftLayout.AddComponent(playerRowLayout);
            }

            // Create buttons
            roomIdTextBox = new TextBox {
                Position = Vector2.Zero, // Will be set by parent layout
                Size = Vector2.Zero,     // Will be set by parent layout
                IsReadOnly = true,
                PlaceholderText = "Room ID: Waiting...",
                FontSize = 50,
                TextColor = Color.Black,
                BackgroundColor = Color.White,
                BorderColor = Color.Black,
                BorderWidth = 2,
                Padding = 10,
                TextAlignment = ContentAlignment.MiddleCenter,
            };

            addBotButton = new Button() {
                Position = Vector2.Zero,
                Size = Vector2.Zero,
                OnClick = () => NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.AddBot)),
                Text = "Add Bot",
                TextColor = Color.White,
                BackgroundColor = Color.Blue,
            };

            startButton = new Button() {
                Position = Vector2.Zero, // Will be set by parent layout
                Size = Vector2.Zero,     // Will be set by parent layout
                OnClick = StartGame,
                Text = "Start Game",
                TextColor = Color.White,
                BackgroundColor = Color.Green,
            };

            leaveButton = new Button() {
                Position = Vector2.Zero, // Will be set by parent layout
                Size = Vector2.Zero,     // Will be set by parent layout
                OnClick = LeaveLobby,
                Text = "Leave",
                TextColor = Color.White,
                BackgroundColor = Color.Red,
            };

            rightLayout.AddComponent(roomIdTextBox);
            rightLayout.AddComponent(addBotButton);
            rightLayout.AddComponent(startButton);
            rightLayout.AddComponent(leaveButton);
            mainLayout.AddComponent(leftLayout);
            mainLayout.AddComponent(rightLayout);
            uiManager.AddComponent(mainLayout, 0);
        }

        public override void Activate() {
            base.Activate();
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetRoomInfo));
        }

        private void StartGame() {
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.StartGame));
        }

        private void LeaveLobby() {
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.LeaveRoom));
            ScreenManager.Instance.NavigateBack();
        }

        public override void HandleResponse(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.RoomInfo: {
                        roomIdTextBox.Text = $"Room ID: {message.Data["roomId"]}";
                        int[] playerIds = Array.ConvertAll(message.Data["playerIds"].Split(';'), int.Parse);
                        int hostId = int.Parse(message.Data["hostId"]);
                        bool isHost = bool.Parse(message.Data["isHost"]);
                        for (int i = 0; i < playerTextBoxes.Length; i++) {
                            if (i < playerIds.Length) {
                                playerTextBoxes[i].Text = playerIds[i].ToString();
                                if (playerIds[i] == hostId) {
                                    playerTextBoxes[i].Text += " (Host)";
                                }
                            } else {
                                playerTextBoxes[i].Text = "";
                                playerTextBoxes[i].PlaceholderText = $"Player {i + 1} (Waiting...)";
                            }
                        }
                    }
                    break;
                case ServerMessageType.PlayerJoined: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        for (int i = 0; i < playerTextBoxes.Length; i++) {
                            if (string.IsNullOrEmpty(playerTextBoxes[i].Text)) {
                                playerTextBoxes[i].Text = playerId.ToString();
                                break;
                            }
                        }
                    }
                    break;
                case ServerMessageType.PlayerLeft: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        for (int i = 0; i < playerTextBoxes.Length; i++) {
                            if (playerTextBoxes[i].Text == playerId.ToString()) {
                                playerTextBoxes[i].Text = "";
                                playerTextBoxes[i].PlaceholderText = $"Player {i + 1} (Waiting...)";
                                break;
                            }
                        }

                        for (int i = 0; i < playerTextBoxes.Length - 1; i++) {
                            if (string.IsNullOrEmpty(playerTextBoxes[i].Text) && !string.IsNullOrEmpty(playerTextBoxes[i + 1].Text)) {
                                playerTextBoxes[i].Text = playerTextBoxes[i + 1].Text;
                                playerTextBoxes[i + 1].Text = "";
                                playerTextBoxes[i + 1].PlaceholderText = $"Player {i + 2} (Waiting...)";
                            }
                        }
                    }
                    break;
                case ServerMessageType.NewHost: {
                        int newHostId = int.Parse(message.Data["hostId"]);
                        for (int i = 0; i < playerTextBoxes.Length; i++) {
                            if (playerTextBoxes[i].Text == newHostId.ToString()) {
                                playerTextBoxes[i].Text += " (Host)";
                            } else {
                                playerTextBoxes[i].Text = playerTextBoxes[i].Text.Replace(" (Host)", "");
                            }
                        }
                    }
                    break;
                case ServerMessageType.GameStarted: {
                        ScreenManager.Instance.NavigateTo(ScreenName.MainGameScreen);
                    }
                    break;
                case ServerMessageType.Error: {
                        string errorMessage = message.Data["message"];
                        Console.WriteLine($"Error: {errorMessage}");
                    }
                    break;
            }
        }
    }
}