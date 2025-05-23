using System;
using Microsoft.Xna.Framework;
using TextCopy;

using Shared;
using Client.Component;
using Client.ContentHolder;
using Client.Network;
using Client.Audio;

namespace Client.Screen {
    public class LobbyScreen : GameScreen {
        private bool _isHost = false;
        private string _currentName = string.Empty;
        private readonly int[] _playerIds = new int[4];
        private ImageView _crownIcon;
        private readonly bool[] _inGames = new bool[4];
        private readonly TextBox[] _playerNames = new TextBox[4];
        private readonly Button[] _kickButtons = new Button[4];
        private readonly ImageView[] _inGameIcons = new ImageView[4];
        private readonly ImageView[] _blankIcons = new ImageView[4];
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
            uiManager.AddComponent(layout);

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
                TextColor = Color.Black,
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
                    AllowedCharacters = CharacterSet.Alphanumeric | CharacterSet.Underscore,
                    Text = "",
                    PlaceholderText = $"Waiting...",
                    TextColor = Color.Black,
                    MaxLength = 10,
                    Gravity = Gravity.Center,
                };
                playerLayout.AddComponent(_playerNames[i]);

                if (i == 0) {
                    _crownIcon = new ImageView() {
                        Width = 80,
                        HeightMode = SizeMode.MatchParent,
                        Texture = TextureHolder.Get("Item/Crown"),
                        ScaleType = ScaleType.FitCenter,
                    };
                    playerLayout.AddComponent(_crownIcon);
                } else {
                    int index = i;
                    _kickButtons[i] = new Button() {
                        Width = 80,
                        HeightMode = SizeMode.MatchParent,
                        Text = "X",
                        OnClick = () => KickPlayer(index),
                        IsEnabled = false,
                        IsVisible = false,
                    };
                    playerLayout.AddComponent(_kickButtons[i]);

                    _blankIcons[i] = new ImageView() {
                        Width = 80,
                        HeightMode = SizeMode.MatchParent,
                        Texture = TextureHolder.Get("Other/Blank"),
                        ScaleType = ScaleType.FitCenter,
                    };
                    playerLayout.AddComponent(_blankIcons[i]);
                }

                _inGameIcons[i] = new ImageView() {
                    Width = 80,
                    HeightMode = SizeMode.MatchParent,
                    Texture = TextureHolder.Get("Item/Bomb"),
                    ScaleType = ScaleType.FitCenter,
                    IsVisible = false,
                };
                playerLayout.AddComponent(_inGameIcons[i]);
            }

            _addBotButton = new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Add Bot",
                OnClick = AddBot,
                IsVisible = false,
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
                Text = "Copy Code",
                OnClick = () => {
                    ClipboardService.SetText(_roomIdText.Text.Replace("Room ", ""));
                    ToastManager.Instance.ShowToast("Room code copied");
                },
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
                IsVisible = false,
            };
            rightLayout.AddComponent(_startButton);

            _waitText = new TextBox() {
                WidthMode = SizeMode.MatchParent,
                Weight = 1,
                Text = "",
                TextColor = Color.Black,
                Gravity = Gravity.Center,
                IsVisible = true,
                IsReadOnly = true,
                IsMultiline = true,
                Padding = 20,
            };
            rightLayout.AddComponent(_waitText);

            for (int i = 0; i < _playerIds.Length; i++) {
                _playerIds[i] = -1;
                _inGames[i] = false;
            }
        }

        public override void Activate() {
            base.Activate();

            MusicPlayer.Play("Adventure");

            for (int i = 0; i < 4; i++) {
                _playerIds[i] = -1;
                _playerNames[i].Text = "";
                _playerNames[i].PlaceholderText = "Waiting...";
                _inGames[i] = false;
            }

            _crownIcon.IsVisible = true;
            _inGameIcons[0].IsVisible = false;
            for (int i = 1; i < 4; i++) {
                _kickButtons[i].IsVisible = false;
                _kickButtons[i].IsEnabled = false;
                _inGameIcons[i].IsVisible = false;
                _blankIcons[i].IsVisible = true;
            }

            _startButton.IsVisible = false;
            _waitText.IsVisible = true;

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
            ScreenManager.Instance.StartLoading();
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.LeaveRoom));
            ScreenManager.Instance.NavigateBack();
        }

        private void ResetPermissions() {
            _crownIcon.IsVisible = !_inGames[0];
            _inGameIcons[0].IsVisible = _inGames[0];
            for (int i = 1; i < 4; i++) {
                _kickButtons[i].IsVisible = _playerIds[i] != -1 && !_inGames[i] && _isHost;
                _kickButtons[i].IsEnabled = _isHost && _playerIds[i] != -1;
                _inGameIcons[i].IsVisible = _inGames[i];
                _blankIcons[i].IsVisible = _playerIds[i] == -1 || (!_inGames[i] && !_isHost);
            }

            _addBotButton.IsVisible = _isHost;
            _startButton.IsVisible = _isHost;
            _waitText.IsVisible = !_isHost;

            bool anyInGame = false;
            for (int i = 0; i < 4; i++) {
                _playerNames[i].IsReadOnly = _playerIds[i] != NetworkManager.Instance.ClientId;
                _playerNames[i].TextColor = _playerIds[i] == NetworkManager.Instance.ClientId ? Color.Red : Color.Black;
                anyInGame |= _inGames[i];
            }

            if (anyInGame) {
                _waitText.Text = "Waiting for players to finish...";
                _startButton.IsEnabled = false;
            } else {
                _waitText.Text = "Waiting for host to start game...";
                _startButton.IsEnabled = true;
            }
        }

        public override void HandleResponse(NetworkMessage message) {
            base.HandleResponse(message);

            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.RoomInfo: {
                        _roomIdText.Text = $"Room {message.Data["roomId"]}";
                        int[] playerIds = Array.ConvertAll(message.Data["playerIds"].Split(';'), int.Parse);
                        string[] usernames = message.Data["usernames"].Split(';');
                        int hostId = int.Parse(message.Data["hostId"]);
                        _isHost = bool.Parse(message.Data["isHost"]);
                        bool[] inGames = Array.ConvertAll(message.Data["inGames"].Split(';'), bool.Parse);

                        for (int i = 0; i < playerIds.Length; i++) {
                            _playerIds[i] = playerIds[i];
                            _playerNames[i].Text = usernames[i];
                            _playerNames[i].PlaceholderText = "";
                            _inGames[i] = inGames[i];

                            if (playerIds[i] == NetworkManager.Instance.ClientId) {
                                _currentName = usernames[i];
                            }
                        }

                        for (int i = 0; i < 4; i++) {
                            if (_playerIds[i] == hostId) {
                                (_playerIds[0], _playerIds[i]) = (_playerIds[i], _playerIds[0]);
                                (_playerNames[0].Text, _playerNames[i].Text) = (_playerNames[i].Text, _playerNames[0].Text);
                                (_playerNames[0].PlaceholderText, _playerNames[i].PlaceholderText) = (_playerNames[i].PlaceholderText, _playerNames[0].PlaceholderText);
                                (_inGames[0], _inGames[i]) = (_inGames[i], _inGames[0]);
                            }
                        }

                        ResetPermissions();

                        ScreenManager.Instance.StopLoading();
                    }
                    break;
                case ServerMessageType.UsernameSet: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        string username = message.Data["username"];

                        if (playerId == NetworkManager.Instance.ClientId) {
                            return;
                        }

                        for (int i = 0; i < 4; i++) {
                            if (_playerIds[i] == playerId) {
                                _playerNames[i].Text = username;
                                break;
                            }
                        }
                    }
                    break;
                case ServerMessageType.PlayerKicked: {
                        ScreenManager.Instance.StartLoading();
                        ScreenManager.Instance.NavigateBack();
                        ToastManager.Instance.ShowToast("You have been kicked");
                    }
                    break;
                case ServerMessageType.PlayerJoined: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        string username = message.Data["username"];
                        for (int i = 0; i < 4; i++) {
                            if (_playerIds[i] == -1) {
                                _playerIds[i] = playerId;
                                _playerNames[i].Text = username;
                                _playerNames[i].PlaceholderText = "";
                                break;
                            }
                        }

                        ResetPermissions();
                    }
                    break;
                case ServerMessageType.PlayerLeft: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        for (int i = 0; i < 4; i++) {
                            if (playerId == _playerIds[i]) {
                                _playerIds[i] = -1;
                                _playerNames[i].Text = "";
                                _playerNames[i].PlaceholderText = "Waiting...";
                                _kickButtons[i].IsEnabled = false;
                                break;
                            }
                        }

                        for (int i = 0; i < 3; i++) {
                            if (_playerIds[i] == -1 && _playerIds[i + 1] != -1) {
                                _playerIds[i] = _playerIds[i + 1];
                                _playerNames[i].Text = _playerNames[i + 1].Text;
                                _playerNames[i].PlaceholderText = _playerNames[i + 1].PlaceholderText;
                                _inGames[i] = _inGames[i + 1];
                                _playerIds[i + 1] = -1;
                                _playerNames[i + 1].Text = "";
                                _playerNames[i + 1].PlaceholderText = "Waiting...";
                                _kickButtons[i + 1].IsEnabled = false;
                                _inGames[i + 1] = false;
                            }
                        }

                        ResetPermissions();
                    }
                    break;
                case ServerMessageType.GameLeft: {
                        int playerId = int.Parse(message.Data["playerId"]);

                        for (int i = 0; i < 4; i++) {
                            if (playerId == _playerIds[i]) {
                                _inGames[i] = false;
                                break;
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

                        for (int i = 0; i < 4; i++) {
                            if (_playerIds[i] == newHostId) {
                                (_playerIds[0], _playerIds[i]) = (_playerIds[i], _playerIds[0]);
                                (_playerNames[0].Text, _playerNames[i].Text) = (_playerNames[i].Text, _playerNames[0].Text);
                                (_playerNames[0].PlaceholderText, _playerNames[i].PlaceholderText) = (_playerNames[i].PlaceholderText, _playerNames[0].PlaceholderText);
                            }
                        }

                        ResetPermissions();

                        if (_isHost) {
                            ToastManager.Instance.ShowToast("You are now the host");
                        }
                    }
                    break;
                case ServerMessageType.GameStarted: {
                        ScreenManager.StartLoading();
                        ScreenManager.Instance.NavigateTo(ScreenName.MainGameScreen);
                    }
                    break;
                case ServerMessageType.Error: {
                        ToastManager.Instance.ShowToast(message.Data["message"]);
                    }
                    break;
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            _addBotButton.IsEnabled = _playerIds[3] == -1;

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