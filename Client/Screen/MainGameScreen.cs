using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Shared;
using Client.Component;
using Client.PowerUps;
using Client.Handler;
using Client.Scene;
using Client.Audio;
using Client.Network;

namespace Client.Screen {
    public class MainGameScreen : GameScreen {
        private readonly object _lock = new();
        private LinearLayout _sidebar;
        private Scoreboard _scoreboard;
        private PowerSlot _powerSlot;
        private MapRenderInfo _map;
        private MapComponent _mapComponent;

        // public MainGameScreen() {
        //     Console.WriteLine("MainGameScreen Constructor");
        // }

        public override void Initialize() {
            _map = new MapRenderInfo();
            _sidebar = new LinearLayout() {
                LayoutOrientation = Orientation.Vertical,
                Position = new Vector2(0, 0),
                Width = 240,
                Height = ScreenSize.Y,
                Spacing = 0,
            };
            uiManager.AddComponent(_sidebar);

            _powerSlot = new PowerSlot(_map) {
                WidthMode = SizeMode.MatchParent,
                Weight = 2,
            };

            _scoreboard = new Scoreboard(_map) {
                Position = new Vector2(0, 0),
                Width = 240,
                Height = ScreenSize.Y,
                Weight = 6,
            };

            _sidebar.AddComponent(_scoreboard);
            _sidebar.AddComponent(_powerSlot);

            _sidebar.AddComponent(new Button() {
                WidthMode = SizeMode.MatchParent,
                Height = 80,
                Text = "Leave Game",
                OnClick = () => {
                    ScreenManager.Instance.StartLoading();
                    NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.LeaveGame));
                    ScreenManager.Instance.NavigateBack();
                },
            });

            _mapComponent = new MapComponent(_map) {
                Position = new Vector2(240, 0)
            };
            uiManager.AddComponent(_mapComponent);

            _map.OnMapInitialized = () => {
                _mapComponent.ProcessMap();
                _scoreboard.SetDuration(_map.Duration);
                _scoreboard.SetPlayerData();
            };
        }

        public override void Activate() {
            base.Activate();
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetGameInfo));
        }

        public override void HandleResponse(NetworkMessage message) {
            base.HandleResponse(message);

            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.GameStopped: {
                        ScreenManager.Instance.NavigateTo(ScreenName.EndGameScreen, isOverlay: true, new(){
                            { "skinMapping", _map.GetSkinMapping() },
                        });
                    }
                    break;
                default:
                    try {
                        HandlerFactory.CreateHandler(_map, Enum.Parse<ServerMessageType>(message.Type.Name)).Handle(message);
                    } catch (Exception ex) {
                        Console.WriteLine($"Error handling message {message.Type.Name}: {ex.Message}");
                    }
                    break;
            }
        }

        private void HandleUpdate(GameTime gameTime) {
            int playerId = NetworkManager.Instance.ClientId;
            if (_map == null || !_map.PlayerInfos.ContainsKey(playerId) || !_mapComponent.ContainsPlayer(playerId)) {
                return;
            }

            KeyboardState key = Keyboard.GetState();

            if (true) {
                MovePlayers(key);
            }

            if (key.IsKeyDown(Keys.Space) || key.IsKeyDown(Keys.Enter)) {
                PlaceBomb(key.IsKeyDown(Keys.Space) ? BombType.Normal : BombType.Special);
            }

            if (key.IsKeyDown(Keys.Q) || key.IsKeyDown(Keys.E)) {
                UsePowerUp(key.IsKeyDown(Keys.Q) ? 0 : 1);
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            HandleUpdate(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            base.Draw(gameTime, spriteBatch);
        }

        private void MovePlayers(KeyboardState key) {
            int playerId = NetworkManager.Instance.ClientId;
            Direction direction = Direction.None;
            if (!_mapComponent.IsPlayerMoving(playerId)) {
                if (key.IsKeyDown(Keys.Up)) {
                    direction = Direction.Up;
                } else if (key.IsKeyDown(Keys.Down)) {
                    direction = Direction.Down;
                } else if (key.IsKeyDown(Keys.Left)) {
                    direction = Direction.Left;
                } else if (key.IsKeyDown(Keys.Right)) {
                    direction = Direction.Right;
                }
            }

            if (direction != Direction.None && !_map.IsPlayerMovable(playerId, direction)) {
                _mapComponent.SetDirection(playerId, direction);
                direction = Direction.None;
            }

            if (direction != Direction.None) {
                _map.MovePlayer(playerId, direction);
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.MovePlayer, new() {
                    { "direction", direction.ToString() }
                }));
            }
        }

        private void PlaceBomb(BombType type) {
            var currentPos = _map.GetMySpritePosition();
            var currentIdx = _map.GetMyPosition();
            Position nearestCell = null;
            float minDistance = float.MaxValue;
            lock (_lock) {
                for (int i = currentIdx.X - 2; i <= currentIdx.X + 2; i++) {
                    for (int j = currentIdx.Y - 2; j <= currentIdx.Y + 2; j++) {
                        if (_map.IsInBounds(i, j) && _map.GetTile(i, j) == TileType.Empty && !_map.HasBombAt(i, j)) {
                            float distance = Math.Abs(currentPos.Y - (i * GameValues.TILE_SIZE)) + Math.Abs(currentPos.X - (j * GameValues.TILE_SIZE));
                            if (distance < GameValues.TILE_SIZE / 3 && distance < minDistance) {
                                minDistance = distance;
                                nearestCell = new Position(i, j);
                            }
                        }
                    }
                }
            }

            if (nearestCell != null) {
                lock (_lock) {
                    if ((_map.BombCount >= GameplayConfig.MaxBombs && !_map.HasActivePowerUp(PowerName.MoreBombs)) || !_map.LockTile(nearestCell.X, nearestCell.Y)) {
                        // if (_map.BombCount >= GameplayConfig.MaxBombs && !_map.HasActivePowerUp(PowerName.MoreBombs)) {
                        //     Console.WriteLine("Bomb limit reached");
                        // } else {
                        //     Console.WriteLine("Tile is locked");
                        // }
                        return;
                    }
                }

                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                    { "x", nearestCell.X.ToString() },
                    { "y", nearestCell.Y.ToString() },
                    { "type", type.ToString() },
                }));
            }
        }

        private void UsePowerUp(int slotNum) {
            var powerSlots = _map.PowerUps;
            if (slotNum < 0 || slotNum >= powerSlots.Length) {
                return;
            }
            var power = powerSlots[slotNum];
            if (power.Item1 != PowerName.None && power.Item2 > 0 && _map.LockPowerSlot(slotNum)) {
                PowerUp powerUp = PowerUpFactory.CreatePowerUp(power.Item1, _map);
                Dictionary<string, object> parameters = powerUp.Use();
                if (parameters == null) {
                    _map.UnlockPowerSlot(slotNum);
                    return;
                }
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.UsePowerUp, new() {
                    { "powerUpType", power.Item1.ToString() },
                    { "slotNum", slotNum.ToString() },
                    { "parameters", JsonSerializer.Serialize(parameters) },
                }));
            }
        }
    }
}