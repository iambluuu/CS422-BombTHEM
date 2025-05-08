using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Client.Component {
    public enum TextWrapping {
        NoWrap,
        Wrap,
        WrapWithOverflow
    }

    public class TextComponent : IComponent {
        public string Text { get; set; } = string.Empty;
        public SpriteFont Font { get; set; } = FontHolder.Get("Font/PressStart2P");
        public Color TextColor { get; set; } = Color.White;
        public Color OutlineColor { get; set; } = Color.Transparent;
        public int OutlineThickness { get; set; } = 0;
        public ContentAlignment TextAlignment { get; set; } = ContentAlignment.MiddleLeft;
        public float FontSize { get; set; } = 1.0f;
        public float LineSpacing { get; set; } = 1.0f;
        public TextWrapping Wrapping { get; set; } = TextWrapping.NoWrap;
        public Padding Padding { get; set; } = new Padding(5);
        public bool IsShadowEnabled { get; set; } = false;
        public Vector2 ShadowOffset { get; set; } = new Vector2(2, 2);
        public Color ShadowColor { get; set; } = new Color(0, 0, 0, 128);

        // For animated text effects
        private float _elapsedTime = 0;
        private bool _isAnimating = false;
        private int _currentCharIndex = 0;
        private float _charRevealSpeed = 30f; // Characters per second
        private string _displayedText = string.Empty;

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (_isAnimating) {
                _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                int charsToShow = (int)(_elapsedTime * _charRevealSpeed);
                if (charsToShow > Text.Length) {
                    charsToShow = Text.Length;
                    _isAnimating = false;
                }
                _displayedText = Text.Substring(0, charsToShow);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible || string.IsNullOrEmpty(Text) || Font == null) return;

            string textToRender = _isAnimating ? _displayedText : Text;
            if (string.IsNullOrEmpty(textToRender)) return;

            // Calculate scale based on font size
            float scale = FontSize;

            // Process text based on wrapping mode
            string[] lines;
            if (Wrapping == TextWrapping.NoWrap) {
                lines = new[] { textToRender };
            } else {
                lines = WrapText(textToRender, (Size.X - Padding.Left - Padding.Right) / scale);
            }

            float lineHeight = Font.LineSpacing * scale * LineSpacing;
            float totalTextHeight = lineHeight * lines.Length;

            // Calculate starting Y position based on alignment
            Vector2 position = Position;
            switch (TextAlignment) {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    position.Y += Padding.Top;
                    break;
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.MiddleRight:
                    position.Y += (Size.Y - totalTextHeight) / 2;
                    break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    position.Y += Size.Y - totalTextHeight - Padding.Bottom;
                    break;
            }

            // Draw each line
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                Vector2 lineSize = Font.MeasureString(line) * scale;

                // Calculate X position based on alignment
                float lineX = position.X;
                switch (TextAlignment) {
                    case ContentAlignment.TopLeft:
                    case ContentAlignment.MiddleLeft:
                    case ContentAlignment.BottomLeft:
                        lineX += Padding.Left;
                        break;
                    case ContentAlignment.TopCenter:
                    case ContentAlignment.MiddleCenter:
                    case ContentAlignment.BottomCenter:
                        lineX += (Size.X - lineSize.X) / 2;
                        break;
                    case ContentAlignment.TopRight:
                    case ContentAlignment.MiddleRight:
                    case ContentAlignment.BottomRight:
                        lineX += Size.X - lineSize.X - Padding.Right;
                        break;
                }

                Vector2 linePosition = new Vector2(lineX, position.Y + i * lineHeight);

                // Draw shadow if enabled
                if (IsShadowEnabled) {
                    spriteBatch.DrawString(Font, line, linePosition + ShadowOffset, ShadowColor,
                        0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }

                // Draw outline if enabled
                if (OutlineThickness > 0) {
                    DrawTextWithOutline(spriteBatch, Font, line, linePosition, TextColor, OutlineColor, OutlineThickness, scale);
                } else {
                    // Draw regular text
                    spriteBatch.DrawString(Font, line, linePosition, TextColor * Opacity,
                        0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
        }

        private void DrawTextWithOutline(SpriteBatch spriteBatch, SpriteFont font, string text,
            Vector2 position, Color textColor, Color outlineColor, int thickness, float scale) {
            // Draw outline
            for (int x = -thickness; x <= thickness; x++) {
                for (int y = -thickness; y <= thickness; y++) {
                    if (x != 0 || y != 0) {
                        Vector2 offset = new Vector2(x, y);
                        spriteBatch.DrawString(font, text, position + offset, outlineColor * Opacity,
                            0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    }
                }
            }

            // Draw text
            spriteBatch.DrawString(font, text, position, textColor * Opacity,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private string[] WrapText(string text, float maxLineWidth) {
            string[] words = text.Split(' ');
            var lines = new System.Collections.Generic.List<string>();
            string currentLine = string.Empty;

            foreach (string word in words) {
                string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
                Vector2 size = Font.MeasureString(testLine);

                if (size.X <= maxLineWidth || currentLine.Length == 0) {
                    currentLine = testLine;
                } else {
                    lines.Add(currentLine);
                    currentLine = word;
                }
            }

            if (currentLine.Length > 0) {
                lines.Add(currentLine);
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Animates the text to be revealed character by character
        /// </summary>
        /// <param name="speed">Characters per second</param>
        public void AnimateText(float speed = 30f) {
            _isAnimating = true;
            _elapsedTime = 0;
            _charRevealSpeed = speed;
            _displayedText = string.Empty;
        }

        /// <summary>
        /// Stops any text animation and shows the full text
        /// </summary>
        public void StopAnimation() {
            _isAnimating = false;
            _displayedText = Text;
        }

        /// <summary>
        /// Returns the total height needed to display the current text with the current settings
        /// </summary>
        public float GetTextHeight() {
            if (string.IsNullOrEmpty(Text) || Font == null) return 0;

            float scale = FontSize;
            string[] lines;

            if (Wrapping == TextWrapping.NoWrap) {
                lines = new[] { Text };
            } else {
                lines = WrapText(Text, (Size.X - Padding.Left - Padding.Right) / scale);
            }

            return Font.LineSpacing * scale * LineSpacing * lines.Length;
        }

        /// <summary>
        /// Adjusts the component's height to fit the text content
        /// </summary>
        public void AutoSize() {
            float height = GetTextHeight() + Padding.Top + Padding.Bottom;
            Size = new Vector2(Size.X, height);
        }
    }
}