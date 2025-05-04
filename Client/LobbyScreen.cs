using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Client.Component;
using Shared;

namespace Client {
    public class LobbyScreen : GameScreen {
        private TextBox[] playerTextBoxes;
        private Button[] kickButtons;
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
                Size = new Vector2(800, 600),
                Padding = 20,
            };
            mainLayout.Center(new Rectangle(0, 0, Client.Instance.GraphicsDevice.Viewport.Width, Client.Instance.GraphicsDevice.Viewport.Height));

            leftLayout = new LinearLayout(LinearLayout.Orientation.Vertical, spacing: 15) {
                Position = Vector2.Zero,
                Size = Vector2.Zero,
                Padding = 10,
            };

            // Create right layout for buttons
            rightLayout = new LinearLayout(LinearLayout.Orientation.Vertical, spacing: 15) {
                Position = Vector2.Zero,
                Size = Vector2.Zero,
                Padding = 10,
            };

            playerTextBoxes = new TextBox[4];
            kickButtons = new Button[4];
            for (int i = 0; i < 4; i++) {
                LinearLayout playerRowLayout = new LinearLayout(LinearLayout.Orientation.Horizontal, spacing: 10) {
                    Position = Vector2.Zero,
                    Size = Vector2.Zero,
                    Padding = 10,
                };

                playerTextBoxes[i] = new TextBox {
                    Position = Vector2.Zero,
                    Size = Vector2.Zero,
                    IsReadOnly = true,
                    PlaceholderText = $"Player {i + 1} (Waiting...)",
                    TextColor = Color.Black,
                    BackgroundColor = Color.White,
                    BorderColor = Color.Black,
                    BorderWidth = 2,
                    Padding = 10,
                    TextAlignment = ContentAlignment.MiddleCenter,
                };

                int index = i;
                kickButtons[i] = new Button() {
                    Position = Vector2.Zero,
                    Size = Vector2.Zero,
                    OnClick = () => KickPlayer(index),
                    Text = "X",
                };

                // Add components to the player row
                playerRowLayout.AddComponent(playerTextBoxes[i]);
                playerRowLayout.AddComponent(kickButtons[i]);

                // Add the row to the left layout
                leftLayout.AddComponent(playerRowLayout);
            }

            // Create buttons
            roomIdTextBox = new TextBox {
                Position = Vector2.Zero,
                Size = Vector2.Zero,
                IsReadOnly = true,
                PlaceholderText = "Room ID: Waiting...",
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
            };

            startButton = new Button() {
                Position = Vector2.Zero,
                Size = Vector2.Zero,
                OnClick = StartGame,
                Text = "Start Game",
            };

            leaveButton = new Button() {
                Position = Vector2.Zero,
                Size = Vector2.Zero,
                OnClick = LeaveLobby,
                Text = "Leave",
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

        private void KickPlayer(int index) {
            if (string.IsNullOrEmpty(playerTextBoxes[index].Text)) {
                return;
            }

            int playerId = int.Parse(playerTextBoxes[index].Text.Replace(" (Host)", ""));

            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.KickPlayer, new() {
                { "playerId", playerId.ToString() },
            }));
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
                case ServerMessageType.PlayerKicked: {
                        ScreenManager.Instance.NavigateToRoot();
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