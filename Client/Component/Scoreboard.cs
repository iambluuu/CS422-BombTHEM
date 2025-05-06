using Microsoft.VisualBasic.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;

namespace Client.Component {
    public class Scoreboard : IComponent {
        private List<ScoreboardEntry> _entries = new();
        private const int MaxEntryNum = 4;
        private const float TimePerGame = 120f; // seconds
        private const int Padding = 25; // surrounding padding
        private const int Spacing = 5; // spacing between entries
        private const int SeparatorHeight = 2; // height of the separator line
        private float ClockHeight = 30; // height of the clock display

        // For drawing entries
        private static Vector2 EntrySize;
        private static Vector2 TopLeftPosition;

        public override Vector2 Size {
            get => base.Size;
            set {
                base.Size = value;
                float clockScale = (Size.X - Padding * 2 - 20) / _font.MeasureString("00:00").X;
                ClockHeight = _font.MeasureString("00:00").Y * clockScale;

                var entryWidth = Size.X - Padding * 2;
                var entryHeight = entryWidth * 0.5f;
                EntrySize = new Vector2(entryWidth, entryHeight);
                TopLeftPosition = new Vector2(Position.X + Padding, Position.Y + Padding + ClockHeight + Spacing);
                for (int i = 0; i < _entries.Count; i++) {
                    _entries[i].Rank = i; // Update rank based on new size
                }
            }
        }

        // Nine-slice texture size
        private static readonly Vector2 CornerSize = new Vector2(5, 5);
        private static readonly Rectangle TextureSize = new(0, 0, 16, 16); // Assuming the texture is 16x16 pixels

        private float _timer = TimePerGame;
        private Color _textColor = Color.White;

        private readonly Texture2D _backgroundTexture;
        private readonly Texture2D _borderTexture;
        private readonly SpriteFont _font;

        // Receive a list of player name and skin
        public Scoreboard(List<(string, int)> playerData) {
            for (int i = 0; i < Math.Min(playerData.Count, MaxEntryNum); i++) {
                _entries.Add(new ScoreboardEntry(playerData[i].Item1, playerData[i].Item2, rank: i));
            }

            _backgroundTexture = TextureHolder.Get("Texture/Theme/nine_path_bg_2");
            _borderTexture = TextureHolder.Get("Texture/Theme/scoreboard_border");
            _font = FontHolder.Get("Font/NormalFont");

            float clockScale = (Size.X - Padding * 2 - 20) / _font.MeasureString("00:00").X;
            ClockHeight = _font.MeasureString("00:00").Y * clockScale;

            TopLeftPosition = new Vector2(Position.X + Padding, Position.Y + Padding + ClockHeight + Spacing);
        }

        public void IncreaseScore(int index) {
            if (index >= 0 && index < _entries.Count) {
                _entries[index].Score++;
            }

            _entries.Sort((a, b) => b.Score.CompareTo(a.Score)); // Sort by score descending
            for (int i = 0; i < _entries.Count; i++) {
                if (_entries[i].Rank != i) {
                    _entries[i].Rank = i;
                }
            }
        }

        public override void Update(GameTime gameTime) {
            _timer = Math.Max(0, _timer - (float)gameTime.ElapsedGameTime.TotalSeconds);
            foreach (var entry in _entries) {
                entry.Update(gameTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            // Draw the scoreboard background
            DrawNineSlice(spriteBatch, _backgroundTexture);

            // Draw the border
            DrawNineSlice(spriteBatch, _borderTexture);

            // Draw the timer
            var timerText = TimeSpan.FromSeconds(_timer).ToString(@"mm\:ss");
            Vector2 timerSize = _font.MeasureString(timerText);
            float timerScale = ClockHeight / timerSize.Y;
            Vector2 timerPosition = new Vector2(Position.X + Size.X / 2, Position.Y + Padding);
            spriteBatch.DrawString(_font, timerText, timerPosition, _textColor, 0f, new Vector2(timerSize.X / 2, 0), timerScale, SpriteEffects.None, 0f);

            // Draw each entry
            Texture2D separatorTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            separatorTexture.SetData(new[] { Color.Black });

            for (int i = 0; i < _entries.Count; i++) {
                // Draw separator line
                var separatorPosition = new Vector2(Position.X + Padding, TopLeftPosition.Y + (EntrySize.Y + Spacing) * i);
                spriteBatch.Draw(separatorTexture, new Rectangle((int)separatorPosition.X, (int)separatorPosition.Y, (int)Size.X - Padding * 2, SeparatorHeight), Color.White);

                // Draw entry
                _entries[i].Draw(spriteBatch);
            }
        }

        private void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture) {
            var scale = (Padding - 5) / CornerSize.X; // Scale factor based on padding and corner size
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
            public int PlayerSkin { get; set; }
            public int Score { get; set; }
            public int Live { get; set; }
            private int _rank;
            public int Rank {
                get => _rank;
                set {
                    _animationTimer = AnimationDuration; // Reset animation timer when rank changes
                    _rank = value;
                    _targetPosition = new Vector2(TopLeftPosition.X, TopLeftPosition.Y + (EntrySize.Y + Scoreboard.Spacing) * value);
                }
            }

            private const int AnimationDuration = 1000; // milliseconds
            private const int Spacing = 5; // spacing between elements of the entry
            private int _animationTimer = 0;
            private Vector2 _currentPosition;
            private Vector2 _targetPosition;

            public ScoreboardEntry(string playerName, int playerSkin, int score = 0, int live = 3, int rank = 0) {
                PlayerName = playerName;
                PlayerSkin = playerSkin;
                Score = score;
                Live = live;
                Rank = rank;

                _currentPosition = new Vector2(TopLeftPosition.X, TopLeftPosition.Y + (EntrySize.Y + Scoreboard.Spacing) * rank);
                _targetPosition = _currentPosition;
            }

            public void Update(GameTime gameTime) {
                // Update logic for each entry if needed
                if (_animationTimer > 0) {
                    _animationTimer -= (int)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (_animationTimer < 0) {
                        _animationTimer = 0;
                    }
                }
                // Update position based on animation timer
                if (_animationTimer > 0) {
                    float t = (float)_animationTimer / AnimationDuration;
                    float EaseOutCubic(float t) => 1 - MathF.Pow(1 - t, 3);
                    _currentPosition = Vector2.Lerp(_targetPosition, _currentPosition, EaseOutCubic(t));
                } else {
                    _currentPosition = _targetPosition;
                }
            }

            public void Draw(SpriteBatch spriteBatch) {
                // Draw icon
                var iconTexture = TextureHolder.Get($"Texture/Character/{(Shared.PlayerSkin)PlayerSkin}", new Rectangle(0, 0, 16, 13));
                var iconSize = Math.Min(EntrySize.Y, EntrySize.X / 2.5f);
                spriteBatch.Draw(
                    iconTexture,
                    new Rectangle((int)_currentPosition.X, (int)(_currentPosition.Y + (EntrySize.Y - iconSize) / 2), (int)iconSize, (int)iconSize),
                    new Rectangle(0, 0, 16, 13),
                    Color.White
                );

                // line height for info display
                var lineHeight = (EntrySize.Y - Spacing) / 2;

                // Draw the player name on top line
                var font = FontHolder.Get("Font/NormalFont");
                var textScale = font.LineSpacing / lineHeight;
                var nameText = PlayerName;
                var namePosition = new Vector2(_currentPosition.X + Spacing + iconSize, _currentPosition.Y);
                spriteBatch.DrawString(font, nameText, namePosition, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

                // Draw the score on bottom line
                var scoreText = Score.ToString();
                var scorePosition = new Vector2(_currentPosition.X + Spacing + iconSize, _currentPosition.Y + EntrySize.Y - font.LineSpacing);
                spriteBatch.DrawString(font, scoreText, scorePosition, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            }
        }
    }
}