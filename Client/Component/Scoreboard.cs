using Microsoft.VisualBasic.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Client.Component {
    public class Scoreboard : IComponent {
        private List<ScoreboardEntry> _entries = new();
        private const int MaxEntryNum = 4;
        private const int Paddings = 25;
        private const int Spacing = 5;
        private const int SeparatorHeight = 2;
        private float ClockHeight = 30;
        private object _lock = new();
        private Stopwatch _stopwatch;
        private double _lastElapsed;
        private static Vector2 EntrySize;
        private static Vector2 TopLeftPosition;

        public override Vector2 Size {
            get => base.Size;
            set {
                base.Size = value;
                float clockScale = (Size.X - Paddings * 2 - 20) / _font.MeasureString("00:00").X;
                ClockHeight = _font.MeasureString("00:00").Y * clockScale;

                var entryWidth = Size.X - Paddings * 2;
                var entryHeight = entryWidth * 0.5f;
                EntrySize = new Vector2(entryWidth, entryHeight);
                TopLeftPosition = new Vector2(Position.X + Paddings, Position.Y + Paddings + ClockHeight + Spacing);
                for (int i = 0; i < _entries.Count; i++) {
                    _entries[i].Rank = i;
                }
            }
        }

        public override float Width { get => base.Width; set => base.Width = value; }
        public override float Height { get => base.Height; set => base.Height = value; }
        private static readonly Vector2 CornerSize = new Vector2(5, 5);
        private static readonly Rectangle TextureSize = new(0, 0, 16, 16);

        private float _timer = 0;
        private Color _textColor = Color.White;

        private readonly Texture2D _backgroundTexture;
        private readonly Texture2D _borderTexture;
        private readonly SpriteFont _font;
        public Scoreboard() {
            _backgroundTexture = TextureHolder.Get("Theme/nine_path_bg_2");
            _borderTexture = TextureHolder.Get("Theme/scoreboard_border");
            _font = FontHolder.Get("PressStart2P");

            float clockScale = (Size.X - Paddings * 2 - 20) / _font.MeasureString("00:00").X;
            ClockHeight = _font.MeasureString("00:00").Y * clockScale;

            TopLeftPosition = new Vector2(Position.X + Paddings, Position.Y + Paddings + ClockHeight + Spacing);
        }

        public void SetDuration(float duration) {
            _timer = duration;
            _stopwatch = Stopwatch.StartNew();
            _lastElapsed = 0;
        }

        public void SetPlayerData(List<(string, string, int)> playerData) {
            lock (_lock) {
                _entries.Clear();
                for (int i = 0; i < Math.Min(playerData.Count, MaxEntryNum); i++) {
                    _entries.Add(new ScoreboardEntry(playerData[i].Item1, playerData[i].Item2, playerData[i].Item3, rank: 0));
                }
            }

            UpdateRanks();
        }

        public void IncreaseScore(int playerId) {
            for (int i = 0; i < _entries.Count; i++) {
                if (_entries[i].PlayerName == playerId.ToString()) {
                    _entries[i].Score++;
                    break;
                }
            }
            UpdateRanks();
        }

        private void UpdateRanks() {
            lock (_lock) {
                _entries.Sort((a, b) => b.Score.CompareTo(a.Score));
                for (int i = 0; i < _entries.Count; i++) {
                    if (_entries[i].Rank != i) {
                        _entries[i].Rank = i;
                    }
                }
            }
        }

        public override void Update(GameTime gameTime) {
            if (_stopwatch != null) {
                double currentElapsed = _stopwatch.Elapsed.TotalSeconds;
                double delta = currentElapsed - _lastElapsed;
                _lastElapsed = currentElapsed;
                _timer = Math.Max(0, _timer - (float)delta);
            }

            lock (_lock) {
                foreach (var entry in _entries) {
                    entry.Update(gameTime);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            DrawNineSlice(spriteBatch, _backgroundTexture);
            DrawNineSlice(spriteBatch, _borderTexture);
            var timerText = TimeSpan.FromSeconds(_timer).ToString(@"mm\:ss");
            Vector2 timerSize = _font.MeasureString(timerText);
            float timerScale = ClockHeight / timerSize.Y;
            Vector2 timerPosition = new Vector2(Position.X + Size.X / 2, Position.Y + Paddings);
            spriteBatch.DrawString(_font, timerText, timerPosition, _textColor, 0f, new Vector2(timerSize.X / 2, 0), timerScale, SpriteEffects.None, 0f);
            Texture2D separatorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            separatorTexture.SetData(new[] { Color.Black * 0.6f });

            lock (_lock) {
                for (int i = 0; i < _entries.Count; i++) {
                    var separatorPosition = new Vector2(Position.X + Paddings, TopLeftPosition.Y + (EntrySize.Y + Spacing) * i);
                    spriteBatch.Draw(separatorTexture, new Rectangle((int)separatorPosition.X, (int)separatorPosition.Y, (int)Size.X - Paddings * 2, SeparatorHeight), Color.White);
                    _entries[i].Draw(spriteBatch);
                }
            }
        }

        private void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture) {
            var scale = (Paddings - 5) / CornerSize.X;
            var scaledCornerSize = CornerSize * scale;

            Rectangle srcTopLeft = new Rectangle(0, 0, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcTop = new Rectangle((int)CornerSize.X, 0, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);
            Rectangle srcTopRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, 0, (int)CornerSize.X, (int)CornerSize.Y);

            Rectangle srcBottomLeft = new Rectangle(0, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottomRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottom = new Rectangle((int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);

            Rectangle srcMiddle = new Rectangle((int)CornerSize.X, (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleLeft = new Rectangle(0, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));

            Rectangle dstTopLeft = new Rectangle((int)Position.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstTop = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);
            Rectangle dstTopRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);

            Rectangle dstBottomLeft = new Rectangle((int)Position.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottomRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottom = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);

            Rectangle dstMiddle = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleLeft = new Rectangle((int)Position.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));

            spriteBatch.Draw(texture, dstTopLeft, srcTopLeft, Color.White);
            spriteBatch.Draw(texture, dstTop, srcTop, Color.White);
            spriteBatch.Draw(texture, dstTopRight, srcTopRight, Color.White);

            spriteBatch.Draw(texture, dstBottomLeft, srcBottomLeft, Color.White);
            spriteBatch.Draw(texture, dstBottom, srcBottom, Color.White);
            spriteBatch.Draw(texture, dstBottomRight, srcBottomRight, Color.White);

            spriteBatch.Draw(texture, dstMiddle, srcMiddle, Color.White);
            spriteBatch.Draw(texture, dstMiddleLeft, srcMiddleLeft, Color.White);
            spriteBatch.Draw(texture, dstMiddleRight, srcMiddleRight, Color.White);
        }

        private class ScoreboardEntry {
            public string PlayerName { get; set; }
            public string Username { get; set; }
            public int PlayerSkin { get; set; }
            public int Score { get; set; }
            public int Live { get; set; }
            private int _rank;
            public int Rank {
                get => _rank;
                set {
                    _animationTimer = AnimationDuration;
                    _rank = value;
                    _targetPosition = new Vector2(TopLeftPosition.X, TopLeftPosition.Y + (EntrySize.Y + Scoreboard.Spacing) * value);
                }
            }
            private static Texture2D _scoreIconTexture;
            private static readonly Color TextColor = new Color(110, 106, 95);
            private static bool _isScoreIconInitialized = false;

            private static void InitializeScoreIcon(GraphicsDevice graphicsDevice) {
                if (!_isScoreIconInitialized) {
                    _scoreIconTexture = TextureHolder.Get("Icon/score");
                    if (_scoreIconTexture != null) {
                        int width = _scoreIconTexture.Width;
                        int height = _scoreIconTexture.Height;
                        Color[] colorData = new Color[width * height];
                        _scoreIconTexture.GetData(colorData);
                        Color[] newColorData = new Color[width * height];

                        for (int i = 0; i < colorData.Length; i++) {
                            byte alpha = colorData[i].A;

                            if (alpha > 0) {
                                newColorData[i] = new Color(
                                    TextColor.R,
                                    TextColor.G,
                                    TextColor.B,
                                    alpha
                                );
                            } else {
                                newColorData[i] = Color.Transparent;
                            }
                        }
                        _scoreIconTexture.SetData(newColorData);
                        _isScoreIconInitialized = true;
                    }
                }
            }

            private const int AnimationDuration = 1000;
            private const int Spacing = 5;
            private int _animationTimer = 0;
            private Vector2 _currentPosition;
            private Vector2 _targetPosition;

            public ScoreboardEntry(string playerName, string username, int playerSkin, int score = 0, int live = 3, int rank = 0) {
                PlayerName = playerName;
                Username = username;
                PlayerSkin = playerSkin;
                Score = score;
                Live = live;
                Rank = rank;

                _currentPosition = new Vector2(TopLeftPosition.X, TopLeftPosition.Y + (EntrySize.Y + Scoreboard.Spacing) * rank);
                _targetPosition = _currentPosition;
            }

            public void Update(GameTime gameTime) {
                if (_animationTimer > 0) {
                    _animationTimer -= (int)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (_animationTimer < 0) {
                        _animationTimer = 0;
                    }
                }
                if (_animationTimer > 0) {
                    float t = (float)_animationTimer / AnimationDuration;
                    float EaseOutCubic(float t) => 1 - MathF.Pow(1 - t, 3);
                    _currentPosition = Vector2.Lerp(_targetPosition, _currentPosition, EaseOutCubic(t));
                } else {
                    _currentPosition = _targetPosition;
                }
            }

            public void Draw(SpriteBatch spriteBatch) {
                var iconTexture = TextureHolder.Get($"Character/{(Shared.PlayerSkin)PlayerSkin}", new Rectangle(0, 0, 16, 13));
                var iconSize = Math.Min(EntrySize.Y, EntrySize.X / 2.5f);
                var centeringOffset = (EntrySize.Y - iconSize) / 2;
                spriteBatch.Draw(
                    iconTexture,
                    new Rectangle((int)_currentPosition.X, (int)(_currentPosition.Y + centeringOffset), (int)iconSize, (int)iconSize),
                    new Rectangle(0, 0, 16, 13),
                    Color.White
                );
                var lineHeight = (EntrySize.Y - Spacing) / 2;
                var lineWidth = EntrySize.X - iconSize - Spacing;
                var font = FontHolder.Get("PressStart2P");
                var nameText = Username;
                var textScale = Math.Min(Math.Min(lineHeight / font.LineSpacing, lineWidth / font.MeasureString(nameText).X), 1);
                var namePosition = new Vector2(_currentPosition.X + Spacing + iconSize, _currentPosition.Y + centeringOffset + Spacing);
                spriteBatch.DrawString(font, nameText, namePosition, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                var scoreIconSize = lineHeight * 0.7f;
                var scoreIconPosition = new Vector2(
                    _currentPosition.X + Spacing + iconSize,
                    _currentPosition.Y + EntrySize.Y - lineHeight
                );
                if (!_isScoreIconInitialized) {
                    InitializeScoreIcon(spriteBatch.GraphicsDevice);
                }

                spriteBatch.Draw(
                    _scoreIconTexture,
                    new Rectangle(
                        (int)scoreIconPosition.X,
                        (int)scoreIconPosition.Y,
                        (int)scoreIconSize,
                        (int)scoreIconSize
                    ),
                    null,
                    Color.White
                );
                var scoreText = Score.ToString();
                var scoreTextSize = font.MeasureString(scoreText);
                var scoreTextScale = scoreIconSize / scoreTextSize.Y * 0.8f;
                var scorePosition = new Vector2(
                    scoreIconPosition.X + scoreIconSize + Spacing * 2,
                    scoreIconPosition.Y + (scoreIconSize - scoreTextSize.Y * scoreTextScale) / 2
                );
                spriteBatch.DrawString(font, scoreText, scorePosition, TextColor, 0f, Vector2.Zero, scoreTextScale, SpriteEffects.None, 0f);
            }
        }
    }
}
