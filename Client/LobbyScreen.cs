using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TextCopy;

using Client.Component;
using Shared;

namespace Client {
    public class LobbyScreen : GameScreen {
        private bool _isHost = false;
        private string _currentName = String.Empty;
        private readonly int[] _playerIds = new int[4];
        private readonly TextBox[] _playerNames = new TextBox[4];
        private readonly Button[] _kickButtons = new Button[4];
        private TextView _roomIdText;
        private Button _addBotButton;
        private Button _startButton;
        private TextBox _waitText;

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
                Width = 900,
                Spacing = 20,
                Gravity = Gravity.CenterHorizontal,
            };
            layout.AddComponent(mainBox);

            _roomIdText = new TextView() {
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Text = "Waiting...",
                TextSize = 2f,
                TextColor = Color.White,
                Gravity = Gravity.Center,
                PaddingBottom = 20,
            };
            mainBox.AddComponent(_roomIdText);

            var mainLayout = new LinearLayout() {
                LayoutOrientation = Orientation.Horizontal,
                WidthMode = SizeMode.MatchParent,
                HeightMode = SizeMode.WrapContent,
                Gravity = Gravity.Center,
                Spacing = 20,
            };
            mainBox.AddComponent(mainLayout);

            var leftLayout = new ContainerBox() {
                LayoutOrientation = Orientation.Vertical,
                Width = 420,
                HeightMode = SizeMode.WrapContent,
                Spacing = 10,
                Padding = 20,
            };
            mainLayout.AddComponent(leftLayout);

            for (int i = 0; i < _playerIds.Length; i++) {
                var playerLayout = new LinearLayout() {
                    LayoutOrientation = Orientation.Horizontal,
                    WidthMode = SizeMode.MatchParent,
                    Height = 80,
                    Spacing = 10,
                };
                leftLayout.AddComponent(playerLayout);

                _playerNames[i] = new TextBox() {
                    HeightMode = SizeMode.MatchParent,
                    Weight = 1,
                    Text = "",
                    PlaceholderText = $"Waiting...",
                    TextColor = (i == 0) ? Color.Red : Color.Black,
                    MaxLength = 10,
                    Gravity = Gravity.Center,
                };
                playerLayout.AddComponent(_playerNames[i]);

                int index = i;
                _kickButtons[i] = new Button() {
                    Width = 80,
                    HeightMode = SizeMode.MatchParent,
                    Text = "X",
                    OnClick = () => KickPlayer(index),
                };
                playerLayout.AddComponent(_kickButtons[i]);
            }

            _addBotButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Add Bot",
                OnClick = AddBot,
            };
            leftLayout.AddComponent(_addBotButton);

            var rightLayout = new ContainerBox() {
                LayoutOrientation = Orientation.Vertical,
                Width = 330,
                HeightMode = SizeMode.MatchParent,
                Spacing = 10,
                Padding = 20,
            };
            mainLayout.AddComponent(rightLayout);

            var copyRoomIdButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Copy Room ID",
                OnClick = () => ClipboardService.SetText(_roomIdText.Text.Replace("Room ", "")),
            };
            rightLayout.AddComponent(copyRoomIdButton);

            var leaveButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Leave Room",
                OnClick = LeaveLobby,
            };
            rightLayout.AddComponent(leaveButton);

            _startButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Weight = 1,
                Text = "Start Game",
                OnClick = StartGame,
            };
            rightLayout.AddComponent(_startButton);

            _waitText = new TextBox() {
                WidthMode = SizeMode.MatchParent,
                Weight = 1,
                Text = "Waiting for host to start game...",
                TextColor = Color.Black,
                Gravity = Gravity.Center,
                IsVisible = false,
                IsReadOnly = true,
                IsMultiline = true,
                Padding = 20,
            };
            rightLayout.AddComponent(_waitText);

            uiManager.AddComponent(layout, 0);

            for (int i = 0; i < _playerIds.Length; i++) {
                _playerIds[i] = -1;
            }
        }

        public override void Activate() {
            base.Activate();
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetRoomInfo));
        }

        private void KickPlayer(int index) {
            if (_playerIds[index] == -1) {
                return;
            }

            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.KickPlayer, new() {
                { "playerId", _playerIds[index].ToString() },
            }));
        }

        private void AddBot() {
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.AddBot));
        }

        private void StartGame() {
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.StartGame));
        }

        private void LeaveLobby() {
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.LeaveRoom));
            ScreenManager.Instance.NavigateBack();
        }

        private void ResetPermissions() {
            for (int i = 0; i < _kickButtons.Length; i++) {
                _kickButtons[i].IsVisible = _isHost;
            }

            _addBotButton.IsVisible = _isHost;
            _startButton.IsVisible = _isHost;
            _waitText.IsVisible = !_isHost;

            for (int i = 0; i < _playerNames.Length; i++) {
                _playerNames[i].IsReadOnly = _playerIds[i] != NetworkManager.Instance.ClientId;
            }
        }

        public override void HandleResponse(NetworkMessage message) {
            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.RoomInfo: {
                        _roomIdText.Text = $"Room {message.Data["roomId"]}";
                        int[] playerIds = Array.ConvertAll(message.Data["playerIds"].Split(';'), int.Parse);
                        string[] usernames = message.Data["usernames"].Split(';');
                        int hostId = int.Parse(message.Data["hostId"]);
                        _isHost = bool.Parse(message.Data["isHost"]);

                        for (int i = 0; i < playerIds.Length; i++) {
                            _playerIds[i] = playerIds[i];
                            if (i < playerIds.Length) {
                                _playerNames[i].Text = usernames[i];
                                _playerNames[i].PlaceholderText = "";
                            }

                            if (playerIds[i] == NetworkManager.Instance.ClientId) {
                                _currentName = usernames[i];
                            }
                        }

                        for (int i = 0; i < _playerIds.Length; i++) {
                            if (_playerIds[i] == hostId) {
                                (_playerIds[0], _playerIds[i]) = (_playerIds[i], _playerIds[0]);
                                (_playerNames[0].Text, _playerNames[i].Text) = (_playerNames[i].Text, _playerNames[0].Text);
                                (_playerNames[0].PlaceholderText, _playerNames[i].PlaceholderText) = (_playerNames[i].PlaceholderText, _playerNames[0].PlaceholderText);
                            }
                        }

                        ResetPermissions();
                    }
                    break;
                case ServerMessageType.UsernameSet: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        string username = message.Data["username"];

                        if (playerId == NetworkManager.Instance.ClientId) {
                            return;
                        }

                        for (int i = 0; i < _playerIds.Length; i++) {
                            if (_playerIds[i] == playerId) {
                                _playerNames[i].Text = username;
                                break;
                            }
                        }
                    }
                    break;
                case ServerMessageType.PlayerKicked: {
                        ScreenManager.Instance.NavigateBack();
                    }
                    break;
                case ServerMessageType.PlayerJoined: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        string username = message.Data["username"];
                        for (int i = 0; i < _playerIds.Length; i++) {
                            if (_playerIds[i] == -1) {
                                _playerIds[i] = playerId;
                                _playerNames[i].Text = username;
                                _playerNames[i].PlaceholderText = "";
                                break;
                            }
                        }
                    }
                    break;
                case ServerMessageType.PlayerLeft: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        for (int i = 0; i < _playerNames.Length; i++) {
                            if (playerId == _playerIds[i]) {
                                _playerIds[i] = -1;
                                _playerNames[i].Text = "";
                                _playerNames[i].PlaceholderText = "Waiting...";
                                break;
                            }
                        }

                        for (int i = 0; i < _playerIds.Length - 1; i++) {
                            if (_playerIds[i] == -1 && _playerIds[i + 1] != -1) {
                                _playerIds[i] = _playerIds[i + 1];
                                _playerNames[i].Text = _playerNames[i + 1].Text;
                                _playerNames[i].PlaceholderText = _playerNames[i + 1].PlaceholderText;
                                _playerIds[i + 1] = -1;
                                _playerNames[i + 1].Text = "";
                                _playerNames[i + 1].PlaceholderText = "Waiting...";
                            }
                        }

                        ResetPermissions();
                    }
                    break;
                case ServerMessageType.NewHost: {
                        int newHostId = int.Parse(message.Data["hostId"]);
                        if (newHostId == NetworkManager.Instance.ClientId) {
                            _isHost = true;
                        } else {
                            _isHost = false;
                        }

                        for (int i = 0; i < _playerIds.Length; i++) {
                            if (_playerIds[i] == newHostId) {
                                (_playerIds[0], _playerIds[i]) = (_playerIds[i], _playerIds[0]);
                                (_playerNames[0].Text, _playerNames[i].Text) = (_playerNames[i].Text, _playerNames[0].Text);
                                (_playerNames[0].PlaceholderText, _playerNames[i].PlaceholderText) = (_playerNames[i].PlaceholderText, _playerNames[0].PlaceholderText);
                            }
                        }

                        ResetPermissions();
                    }
                    break;
                case ServerMessageType.GameStarted: {
                        ScreenManager.Instance.NavigateTo(ScreenName.MainGameScreen);
                    }
                    break;
                case ServerMessageType.Error: {
                        Console.WriteLine($"Error: {message.Data["message"]}");
                    }
                    break;
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            TextBox myUsernameBox = null;
            for (int i = 0; i < _playerNames.Length; i++) {
                if (_playerIds[i] == NetworkManager.Instance.ClientId) {
                    myUsernameBox = _playerNames[i];
                    break;
                }
            }

            if (myUsernameBox == null) {
                return;
            }

            if (!myUsernameBox.IsFocused && _currentName != myUsernameBox.Text) {
                _currentName = myUsernameBox.Text;
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.SetUsername, new() {
                    { "username", _currentName }
                }));
            }
        }
    }
}