using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Shared;
using Client.Component;
using Client.PowerUps;
using System.Net.NetworkInformation;
using System.Linq;

namespace Client {
    public class MainGameScreen : GameScreen {
        private readonly object _lock = new();
        private LinearLayout _sidebar;
        private Scoreboard _scoreboard;
        private PowerSlot _powerSlot;
        private MapRenderInfo _map = new();
        private MapComponent _mapComponent;

        public MainGameScreen() { }

        public override void Initialize() {
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
                Width = 240, /// ???
                Height = ScreenSize.Y, /// ???
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
            _map.OnMapInitialized = () => {
                _mapComponent.ProcessMap();
                _scoreboard.SetDuration(_map.Duration);
            };
            uiManager.AddComponent(_mapComponent);
        }

        public override void Activate() {
            base.Activate();
            NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.GetGameInfo));
        }

        public override void HandleResponse(NetworkMessage message) {
            base.HandleResponse(message);

            switch (Enum.Parse<ServerMessageType>(message.Type.Name)) {
                case ServerMessageType.GameInfo: {
                        // _map = Map.FromString(message.Data["map"]);
                        // int duration = int.Parse(message.Data["duration"]);
                        // int playerCount = int.Parse(message.Data["playerCount"]);
                        // int[] playerIds = Array.ConvertAll(message.Data["playerIds"].Split(';'), int.Parse);
                        // string[] usernames = message.Data["usernames"].Split(';');
                        // Position[] playerPositions = Array.ConvertAll(message.Data["playerPositions"].Split(';'), Position.FromString);
                        // ProcessMap();
                        // List<(string, string, int)> playerData = new();
                        // for (int i = 0; i < playerCount; i++) {
                        //     int playerId = playerIds[i];
                        //     string username = usernames[i];
                        //     int x = playerPositions[i].X;
                        //     int y = playerPositions[i].Y;
                        //     _map.SetPlayerPosition(playerId, x, y);

                        //     PlayerNode playerNode = new(TextureHolder.Get($"Character/{(PlayerSkin)i}"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                        //         Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE)
                        //     };

                        //     _playerLayer.AttachChild(playerNode);
                        //     _playerNodes.Add(playerId, playerNode);
                        //     _skinMapping.Add(playerId, i);
                        //     playerData.Add((playerId.ToString(), username, i));
                        // }

                        // _scoreboard.SetDuration(duration);
                        // _scoreboard.SetPlayerData(playerData);

                        // ScreenManager.Instance.StopLoading();
                    }
                    break;
                case ServerMessageType.PlayerMoved: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        Direction direction = Enum.Parse<Direction>(message.Data["d"]);

                        if (x != _map.PlayerInfos[playerId].Position.X || y != _map.PlayerInfos[playerId].Position.Y) {
                            lock (_lock) {
                                _map.SetPlayerPosition(playerId, x, y);
                                _playerNodes[playerId].MoveTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), direction);
                            }
                        }
                    }
                    break;
                case ServerMessageType.PlayerLeft:
                case ServerMessageType.GameLeft: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        lock (_lock) {
                            _map.PlayerInfos.Remove(playerId);
                            if (_playerNodes.ContainsKey(playerId)) {
                                _playerLayer.DetachChild(_playerNodes[playerId]);
                                _playerNodes.Remove(playerId);
                            }
                        }
                    }
                    break;
                case ServerMessageType.BombPlaced: {
                        // lock (_lock) {
                        //     if (_map.HasBomb(x, y)) {
                        //         _map.RemoveBomb(x, y);
                        //     }
                        //     _map.AddBomb(x, y, type);
                        //     if (byPlayerId == NetworkManager.Instance.ClientId) {
                        //         _bombCount++;
                        //     }
                        //     var newBomb = BombNodeFactory.CreateNode(type);
                        //     newBomb.Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE);
                        //     _bombNodes.Add((x, y), newBomb);
                        //     _bombLayer.AttachChild(newBomb);
                        // }
                    }
                    break;
                case ServerMessageType.BombExploded: {
                        // int x = int.Parse(message.Data["x"]);
                        // int y = int.Parse(message.Data["y"]);
                        // string[] positions = message.Data["positions"].Split(';');
                        // int byPlayerId = int.Parse(message.Data["byPlayerId"]);

                        // lock (_lock) {
                        //     foreach (var pos in positions) {
                        //         int ex = Position.FromString(pos).X;
                        //         int ey = Position.FromString(pos).Y;

                        //         _bombLayer.AttachChild(new ExplosionNode(TextureHolder.Get("Effect/Explosion"), new Vector2(TILE_SIZE, TILE_SIZE)) {
                        //             Position = new Vector2(ey * TILE_SIZE, ex * TILE_SIZE)
                        //         });

                        //         if (_map.GetTile(ex, ey) == TileType.Grass) {
                        //             _bombLayer.DetachChild(_grassNodes[(ex, ey)]);
                        //             _grassNodes.Remove((ex, ey));
                        //             _map.SetTile(ex, ey, TileType.Empty);
                        //         }
                        //     }
                        //     _map.RemoveBomb(x, y);
                        //     if (byPlayerId == NetworkManager.Instance.ClientId) {
                        //         _bombCount--;
                        //     }
                        //     _bombLayer.DetachChild(_bombNodes[(x, y)]);
                        //     _bombNodes.Remove((x, y));
                        // }
                    }
                    break;
                case ServerMessageType.PlayerDied: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        int byPlayerId = int.Parse(message.Data["byPlayerId"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);

                        lock (_lock) {
                            _map.SetPlayerPosition(playerId, x, y);
                            _playerNodes[playerId].Die();
                            _playerNodes[playerId].TeleportTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), Direction.Down);
                            if (playerId != byPlayerId) {
                                IncreaseScore(byPlayerId);
                            }
                        }
                    }
                    break;
                case ServerMessageType.GameStopped: {
                        ScreenManager.Instance.NavigateTo(ScreenName.EndGameScreen, isOverlay: true, new(){
                            { "skinMapping", _map.SkinMapping },
                        });
                    }
                    break;

                case ServerMessageType.PowerUpUsed: {
                        // string powerUpType = message.Data["powerUpType"];
                        // Dictionary<string, object> parameters = message.Data["parameters"] != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(message.Data["parameters"]) : new Dictionary<string, object>();
                        // parameters["vfxLayer"] = _vfxLayer;
                        // parameters["bombLayer"] = _bombLayer;
                        // parameters["bombNodes"] = _bombNodes;
                        // parameters["playerNodes"] = _playerNodes;
                        // parameters["map"] = _map;
                        // PowerUp powerUp = PowerUpFactory.GetPowerUp(Enum.Parse<PowerName>(powerUpType));
                        // lock (_lock) {
                        //     // _powerSlot.PowerUpUsed(Enum.Parse<PowerName>(powerUpType));
                        //     powerUp.Apply(parameters);
                        // }
                    }
                    break;

                case ServerMessageType.ItemSpawned: {
                        // PowerName powerUpType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
                        // int x = int.Parse(message.Data["x"]);
                        // int y = int.Parse(message.Data["y"]);
                        // lock (_lock) {
                        //     ItemNode itemNode = new(powerUpType) {
                        //         Position = new Vector2(y * TILE_SIZE, x * TILE_SIZE),
                        //     };
                        //     _map.AddItem(x, y, powerUpType);
                        //     _itemNodes.Add((x, y), itemNode);
                        //     _itemLayer.AttachChild(itemNode);
                        // }
                    }
                    break;

                case ServerMessageType.PowerUpPickedUp: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        PowerName powerUpType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        lock (_lock) {
                            _map.RemoveItem(x, y);
                            _map.PlayerInfos[playerId].PickUpItem(powerUpType);
                            _itemLayer.DetachChild(_itemNodes[(x, y)]);
                            _itemNodes.Remove((x, y));
                            if (playerId == NetworkManager.Instance.ClientId) {
                                _powerSlot.ObtainPower(powerUpType);
                            }
                        }
                    }
                    break;
                case ServerMessageType.PowerUpExpired: {
                        int playerId = int.Parse(message.Data["playerId"]);
                        PowerName powerType = Enum.Parse<PowerName>(message.Data["powerUpType"]);
                        lock (_lock) {
                            PowerUp powerUp = PowerUpFactory.GetPowerUp(powerType);
                            powerUp.Remove(_playerNodes[playerId]);
                        }
                    }
                    break;
                case ServerMessageType.ItemExpired: {
                        int x = int.Parse(message.Data["x"]);
                        int y = int.Parse(message.Data["y"]);
                        lock (_lock) {
                            _map.RemoveItem(x, y);
                            _itemLayer.DetachChild(_itemNodes[(x, y)]);
                            _itemNodes.Remove((x, y));
                        }
                    }
                    break;
            }
        }

        private void HandleUpdate(GameTime gameTime) {
            int playerId = NetworkManager.Instance.ClientId;
            if (_map == null || !_map.PlayerInfos.ContainsKey(playerId) || !_playerNodes.ContainsKey(playerId)) {
                return;
            }

            KeyboardState key = Keyboard.GetState();

            if (true) {
                Direction direction = Direction.None;
                if (!_playerNodes[playerId].Moving) {
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
                    _playerNodes[playerId].SetDirection(direction);
                    direction = Direction.None;
                }

                if (direction != Direction.None) {
                    _map.MovePlayer(playerId, direction);
                    int x = _map.PlayerInfos[playerId].Position.X;
                    int y = _map.PlayerInfos[playerId].Position.Y;
                    _playerNodes[playerId].MoveTo(new Vector2(y * TILE_SIZE, x * TILE_SIZE), direction);
                    NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.MovePlayer, new() {
                        { "direction", direction.ToString() }
                    }));
                }
            }

            if (key.IsKeyDown(Keys.Space) || key.IsKeyDown(Keys.Enter)) {
                var currentPos = _playerNodes[playerId].Position;
                var currentIdx = _map.PlayerInfos[playerId].Position;
                Position nearestCell = null;
                float minDistance = float.MaxValue;
                lock (_lock) {
                    for (int i = currentIdx.X - 2; i <= currentIdx.X + 2; i++) {
                        for (int j = currentIdx.Y - 2; j <= currentIdx.Y + 2; j++) {
                            if (_map.IsInBounds(i, j) && _map.GetTile(i, j) == TileType.Empty && !_map.HasBomb(i, j)) {
                                float distance = Math.Abs(currentPos.Y - (i * TILE_SIZE)) + Math.Abs(currentPos.X - (j * TILE_SIZE));
                                if (distance < TILE_SIZE / 3 && distance < minDistance) {
                                    minDistance = distance;
                                    nearestCell = new Position(i, j);
                                }
                            }
                        }
                    }
                }

                if (nearestCell != null) {
                    lock (_lock) {
                        if (_bombCount >= GameplayConfig.MaxBombs && !_map.PlayerInfos[playerId].HasPowerUp(PowerName.MoreBombs)) {
                            return;
                        }
                        _map.AddBomb(nearestCell.X, nearestCell.Y, BombType.Normal);
                    }

                    if (key.IsKeyDown(Keys.Space)) {
                        NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                            { "x", nearestCell.X.ToString() },
                            { "y", nearestCell.Y.ToString() },
                            { "type", BombType.Normal.ToString() },
                        }));
                    } else if (key.IsKeyDown(Keys.Enter)) {
                        NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.PlaceBomb, new() {
                            { "x", nearestCell.X.ToString() },
                            { "y", nearestCell.Y.ToString() },
                            { "type", BombType.Special.ToString() },
                        }));
                    }
                }
            }

            if (key.IsKeyDown(Keys.E) || key.IsKeyDown(Keys.Q)) {
                UsePowerUp(key.IsKeyDown(Keys.E) ? 0 : 1);
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            HandleUpdate(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) {
            base.Draw(gameTime, spriteBatch);
        }

        private void UsePowerUp(int slotNum) {
            var powerSlots = _map.PowerUps;
            if (slotNum < 0 || slotNum >= powerSlots.Length) {
                return;
            }
            var power = powerSlots[slotNum];
            if (power.Item1 != PowerName.None && power.Item2 > 0 && _map.LockPowerSlot(slotNum)) {
                PowerUp powerUp = PowerUpFactory.GetPowerUp(power.Item1);
                powerUp.Use();
            }
        }
    }
}