using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Client.Component {
    // Android-like text truncation modes
    public enum TruncateAt {
        None,
        Start,
        Middle,
        End,
        Marquee
    }

    // Android-like text alignment
    public enum TextAlignment {
        ViewStart,   // Left in LTR, Right in RTL
        Center,
        ViewEnd      // Right in LTR, Left in RTL 
    }

    public class TextView : IComponent {
        // Text content and appearance
        private string _text = string.Empty;
        public string Text {
            get => _text;
            set {
                if (_text != value) {
                    _text = value;
                    _needsTextLayout = true;
                }
            }
        }

        // Font properties
        private SpriteFont _font = FontHolder.Get("Font/PressStart2P");
        public SpriteFont Font {
            get => _font;
            set {
                if (_font != value) {
                    _font = value;
                    _needsTextLayout = true;
                }
            }
        }

        private float _textSize = 1.0f;
        public float TextSize {
            get => _textSize;
            set {
                if (_textSize != value) {
                    _textSize = value;
                    _needsTextLayout = true;
                }
            }
        }

        // Text color and style
        public Color TextColor { get; set; } = Color.White;
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsUnderlined { get; set; } = false;

        // Android-like text layout properties
        public TextAlignment TextAlign { get; set; } = TextAlignment.ViewStart;
        public Gravity Gravity { get; set; } = Gravity.TopLeft;

        // Text truncation and ellipsis behavior
        public TruncateAt TruncateAt { get; set; } = TruncateAt.None;
        public int MaxLines { get; set; } = int.MaxValue;
        public bool SingleLine {
            get => MaxLines == 1;
            set => MaxLines = value ? 1 : int.MaxValue;
        }

        // Line spacing (similar to Android's lineSpacingMultiplier and lineSpacingExtra)
        private float _lineSpacingMultiplier = 1.0f;
        public float LineSpacingMultiplier {
            get => _lineSpacingMultiplier;
            set {
                if (_lineSpacingMultiplier != value) {
                    _lineSpacingMultiplier = value;
                    _needsTextLayout = true;
                }
            }
        }

        private float _lineSpacingExtra = 0f;
        public float LineSpacingExtra {
            get => _lineSpacingExtra;
            set {
                if (_lineSpacingExtra != value) {
                    _lineSpacingExtra = value;
                    _needsTextLayout = true;
                }
            }
        }

        // Text effects
        public bool DrawShadow { get; set; } = false;
        public Vector2 ShadowOffset { get; set; } = new Vector2(1, 1);
        public Color ShadowColor { get; set; } = new Color(0, 0, 0, 128);

        // Outline effect (not standard in Android but useful)
        public bool DrawOutline { get; set; } = false;
        public int OutlineWidth { get; set; } = 1;
        public Color OutlineColor { get; set; } = Color.Black;

        // Text animation (like Android's typeface animation)
        private bool _isAnimating = false;
        private float _elapsedTime = 0;
        private float _charRevealSpeed = 30f; // Characters per second
        private string _displayedText = string.Empty;
        private bool _needsTextLayout = true;

        // Calculated text layout information
        private List<string> _lines = new List<string>();
        private float _contentWidth = 0;
        private float _contentHeight = 0;

        // Android-like ellipsis character
        private const string ELLIPSIS = "...";

        // Override MeasureContentWidth for SizeMode.WrapContent
        protected override float MeasureContentWidth() {
            if (string.IsNullOrEmpty(Text) || Font == null)
                return PaddingLeft + PaddingRight;

            // For a single line or non-wrapped text, measure the full text width
            if (SingleLine || TruncateAt != TruncateAt.None) {
                return Font.MeasureString(Text).X * TextSize + PaddingLeft + PaddingRight;
            }

            // For multiline text with no constraints, use the longest line
            float maxWidth = 0;
            foreach (var line in _lines) {
                float lineWidth = Font.MeasureString(line).X * TextSize;
                maxWidth = Math.Max(maxWidth, lineWidth);
            }

            return maxWidth + PaddingLeft + PaddingRight;
        }

        // Override MeasureContentHeight for SizeMode.WrapContent
        protected override float MeasureContentHeight() {
            if (string.IsNullOrEmpty(Text) || Font == null)
                return PaddingTop + PaddingBottom;

            float lineHeight = CalculateLineHeight();

            if (SingleLine) {
                return lineHeight + PaddingTop + PaddingBottom;
            }

            // For multiple lines, calculate total height
            float totalHeight = _lines.Count * lineHeight;
            return totalHeight + PaddingTop + PaddingBottom;
        }

        private float CalculateLineHeight() {
            return Font.LineSpacing * TextSize * LineSpacingMultiplier + LineSpacingExtra;
        }

        // Layout the text when properties change or width constraints change
        private void LayoutText() {
            if (!_needsTextLayout && _lines.Count > 0) return;

            _lines.Clear();

            if (string.IsNullOrEmpty(Text) || Font == null) {
                _contentWidth = 0;
                _contentHeight = 0;
                _needsTextLayout = false;
                return;
            }

            float availableWidth = Width - PaddingLeft - PaddingRight;
            if (availableWidth <= 0) availableWidth = float.MaxValue;

            // Handle single line case
            if (SingleLine) {
                string line = Text;
                if (TruncateAt != TruncateAt.None) {
                    line = TruncateText(Text, availableWidth / TextSize);
                }
                _lines.Add(line);
            } else {
                // Split into lines
                string[] paragraphs = Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string paragraph in paragraphs) {
                    if (_lines.Count >= MaxLines) break;

                    if (availableWidth == float.MaxValue) {
                        // No width constraint
                        _lines.Add(paragraph);
                    } else {
                        // Word wrap based on available width
                        WrapParagraph(paragraph, availableWidth / TextSize);
                    }
                }
            }

            // Calculate content dimensions
            _contentWidth = 0;
            foreach (var line in _lines) {
                _contentWidth = Math.Max(_contentWidth, Font.MeasureString(line).X * TextSize);
            }

            _contentHeight = _lines.Count * CalculateLineHeight();
            _needsTextLayout = false;
        }

        private void WrapParagraph(string paragraph, float maxLineWidth) {
            if (string.IsNullOrEmpty(paragraph)) {
                _lines.Add(string.Empty);
                return;
            }

            string[] words = paragraph.Split(' ');
            string currentLine = string.Empty;

            foreach (string word in words) {
                string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
                Vector2 size = Font.MeasureString(testLine);

                if (size.X <= maxLineWidth || currentLine.Length == 0) {
                    currentLine = testLine;
                } else {
                    _lines.Add(currentLine);
                    if (_lines.Count >= MaxLines) {
                        // Apply ellipsis to the last line if needed
                        if (TruncateAt == TruncateAt.End && _lines.Count > 0) {
                            string lastLine = _lines[_lines.Count - 1];
                            _lines[_lines.Count - 1] = TruncateText(lastLine, maxLineWidth);
                        }
                        return;
                    }
                    currentLine = word;
                }
            }

            if (currentLine.Length > 0) {
                _lines.Add(currentLine);
            }
        }

        private string TruncateText(string text, float maxWidth) {
            if (Font.MeasureString(text).X <= maxWidth) return text;

            switch (TruncateAt) {
                case TruncateAt.Start:
                    return TruncateStart(text, maxWidth);
                case TruncateAt.Middle:
                    return TruncateMiddle(text, maxWidth);
                case TruncateAt.End:
                    return TruncateEnd(text, maxWidth);
                case TruncateAt.Marquee:
                    // Marquee would require animation, for now just truncate at end
                    return TruncateEnd(text, maxWidth);
                default:
                    return text;
            }
        }

        private string TruncateStart(string text, float maxWidth) {
            int length = text.Length;
            string result = text;

            while (length > 0 && Font.MeasureString(ELLIPSIS + result).X > maxWidth) {
                result = text.Substring(text.Length - length + 1);
                length--;
            }

            return ELLIPSIS + result;
        }

        private string TruncateMiddle(string text, float maxWidth) {
            int length = text.Length;

            while (length > 0) {
                int halfLength = length / 2;
                string startPart = text.Substring(0, halfLength);
                string endPart = text.Substring(text.Length - halfLength);
                string result = startPart + ELLIPSIS + endPart;

                if (Font.MeasureString(result).X <= maxWidth || halfLength == 0) {
                    return result;
                }

                length--;
            }

            return ELLIPSIS;
        }

        private string TruncateEnd(string text, float maxWidth) {
            int length = text.Length;
            string result = text;

            while (length > 0 && Font.MeasureString(result + ELLIPSIS).X > maxWidth) {
                result = text.Substring(0, length - 1);
                length--;
            }

            return result + ELLIPSIS;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            // Layout text if needed
            if (_needsTextLayout) {
                LayoutText();
            }

            // Handle text animation
            if (_isAnimating) {
                _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                int charsToShow = (int)(_elapsedTime * _charRevealSpeed);

                if (charsToShow >= Text.Length) {
                    charsToShow = Text.Length;
                    _isAnimating = false;
                }

                _displayedText = Text.Substring(0, charsToShow);
                _needsTextLayout = true; // Re-layout for animated text
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible || Font == null) return;

            // Ensure text is laid out
            if (_needsTextLayout) {
                LayoutText();
            }

            if (_lines.Count == 0) return;

            string[] linesToDraw = _isAnimating ?
                WrapAnimatedText(_displayedText) :
                _lines.ToArray();

            float lineHeight = CalculateLineHeight();
            float totalTextHeight = linesToDraw.Length * lineHeight;

            // Calculate vertical positioning based on gravity
            float startY = Position.Y + PaddingTop;
            if ((Gravity & Gravity.Bottom) != 0) {
                startY = Position.Y + Height - totalTextHeight - PaddingBottom;
            } else if ((Gravity & Gravity.CenterVertical) != 0) {
                startY = Position.Y + (Height - totalTextHeight) / 2;
            }

            // Draw each line
            for (int i = 0; i < linesToDraw.Length; i++) {
                string line = linesToDraw[i];
                float lineWidth = Font.MeasureString(line).X * TextSize;

                // Calculate horizontal positioning based on gravity or textAlign
                float lineX = Position.X + PaddingLeft;

                // Use gravity for horizontal alignment if specified
                if ((Gravity & Gravity.Right) != 0) {
                    lineX = Position.X + Width - lineWidth - PaddingRight;
                } else if ((Gravity & Gravity.CenterHorizontal) != 0) {
                    lineX = Position.X + (Width - lineWidth) / 2;
                }
                  // Otherwise use TextAlign property
                  else if (TextAlign == TextAlignment.Center) {
                    lineX = Position.X + (Width - lineWidth) / 2;
                } else if (TextAlign == TextAlignment.ViewEnd) {
                    lineX = Position.X + Width - lineWidth - PaddingRight;
                }

                float lineY = startY + i * lineHeight;
                Vector2 linePosition = new Vector2(lineX, lineY);

                // Draw shadow if enabled
                if (DrawShadow) {
                    spriteBatch.DrawString(
                        Font,
                        line,
                        linePosition + ShadowOffset,
                        ShadowColor * Opacity,
                        0f, Vector2.Zero, TextSize,
                        SpriteEffects.None, 0f
                    );
                }

                // Draw outline if enabled
                if (DrawOutline) {
                    DrawTextWithOutline(spriteBatch, Font, line, linePosition, TextColor, OutlineColor, OutlineWidth);
                } else {
                    // Draw normal text
                    spriteBatch.DrawString(
                        Font,
                        line,
                        linePosition,
                        TextColor * Opacity,
                        0f, Vector2.Zero, TextSize,
                        SpriteEffects.None, 0f
                    );
                }
            }
        }

        private string[] WrapAnimatedText(string animatedText) {
            // Simplified version just for animated display
            if (string.IsNullOrEmpty(animatedText)) {
                return new string[0];
            }

            float availableWidth = Width - PaddingLeft - PaddingRight;
            if (SingleLine || availableWidth <= 0) {
                return new[] { animatedText };
            }

            // Use simpler wrapping for animated text
            List<string> animatedLines = new List<string>();
            string[] words = animatedText.Split(' ');
            string currentLine = string.Empty;

            foreach (string word in words) {
                string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
                Vector2 size = Font.MeasureString(testLine);

                if (size.X * TextSize <= availableWidth || currentLine.Length == 0) {
                    currentLine = testLine;
                } else {
                    animatedLines.Add(currentLine);
                    currentLine = word;
                }
            }

            if (currentLine.Length > 0) {
                animatedLines.Add(currentLine);
            }

            return animatedLines.ToArray();
        }

        private void DrawTextWithOutline(SpriteBatch spriteBatch, SpriteFont font, string text,
                                         Vector2 position, Color textColor, Color outlineColor, int thickness) {
            // Draw outline with specified thickness
            for (int x = -thickness; x <= thickness; x++) {
                for (int y = -thickness; y <= thickness; y++) {
                    if (x != 0 || y != 0) {
                        Vector2 offset = new Vector2(x, y);
                        spriteBatch.DrawString(
                            font,
                            text,
                            position + offset,
                            outlineColor * Opacity,
                            0f, Vector2.Zero, TextSize,
                            SpriteEffects.None, 0f
                        );
                    }
                }
            }

            // Draw text on top of outline
            spriteBatch.DrawString(
                font,
                text,
                position,
                textColor * Opacity,
                0f, Vector2.Zero, TextSize,
                SpriteEffects.None, 0f
            );
        }

        // Android-like method to force text to be laid out again
        public void RequestLayout() {
            _needsTextLayout = true;
        }

        // Methods for text animation (similar to TypewriterTextView in Android custom implementations)
        public void AnimateText(float charsPerSecond = 30f) {
            _isAnimating = true;
            _elapsedTime = 0;
            _charRevealSpeed = charsPerSecond;
            _displayedText = string.Empty;
        }

        public void StopAnimation() {
            _isAnimating = false;
            _displayedText = Text;
        }

        // Android-like methods for text measurement
        public float GetTextWidth() {
            if (_needsTextLayout) LayoutText();
            return _contentWidth;
        }

        public float GetTextHeight() {
            if (_needsTextLayout) LayoutText();
            return _contentHeight;
        }

        // Called when component size changes
        public override Vector2 Size {
            get => base.Size;
            set {
                if (value != base.Size) {
                    base.Size = value;
                    _needsTextLayout = true;
                }
            }
        }

        // Android-like automatic height adjustment
        public void AutoSize(bool horizontal = false, bool vertical = true) {
            if (_needsTextLayout) LayoutText();

            float newWidth = horizontal ? GetTextWidth() + PaddingLeft + PaddingRight : Width;
            float newHeight = vertical ? GetTextHeight() + PaddingTop + PaddingBottom : Height;

            Size = new Vector2(newWidth, newHeight);
        }
    }
}
