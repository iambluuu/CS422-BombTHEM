using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Client.Component {
    public enum TruncateAt {
        None,
        Start,
        Middle,
        End,
        Marquee
    }
    public enum TextAlignment {
        ViewStart,
        Center,
        ViewEnd
    }

    public class TextView : IComponent {
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
        private SpriteFont _font = FontHolder.Get("PressStart2P");
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
        public Color TextColor { get; set; } = Color.White;
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsUnderlined { get; set; } = false;
        public TextAlignment TextAlign { get; set; } = TextAlignment.ViewStart;
        public Gravity Gravity { get; set; } = Gravity.TopLeft;
        public TruncateAt TruncateAt { get; set; } = TruncateAt.None;
        public int MaxLines { get; set; } = int.MaxValue;
        public bool SingleLine {
            get => MaxLines == 1;
            set => MaxLines = value ? 1 : int.MaxValue;
        }
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
        public bool DrawShadow { get; set; } = false;
        public Vector2 ShadowOffset { get; set; } = new Vector2(1, 1);
        public Color ShadowColor { get; set; } = new Color(0, 0, 0, 128);
        public bool DrawOutline { get; set; } = false;
        public int OutlineWidth { get; set; } = 1;
        public Color OutlineColor { get; set; } = Color.Black;
        private bool _isAnimating = false;
        private float _elapsedTime = 0;
        private float _charRevealSpeed = 30f;
        private string _displayedText = string.Empty;
        private bool _needsTextLayout = true;
        private List<string> _lines = new List<string>();
        private float _contentWidth = 0;
        private float _contentHeight = 0;
        private const string ELLIPSIS = "...";

        protected override float MeasureContentWidth() {
            if (string.IsNullOrEmpty(Text) || Font == null)
                return PaddingLeft + PaddingRight;
            if (SingleLine || TruncateAt != TruncateAt.None) {
                return Font.MeasureString(Text).X * TextSize + PaddingLeft + PaddingRight;
            }
            float maxWidth = 0;
            foreach (var line in _lines) {
                float lineWidth = Font.MeasureString(line).X * TextSize;
                maxWidth = Math.Max(maxWidth, lineWidth);
            }

            return maxWidth + PaddingLeft + PaddingRight;
        }

        protected override float MeasureContentHeight() {
            if (string.IsNullOrEmpty(Text) || Font == null)
                return PaddingTop + PaddingBottom;

            float lineHeight = CalculateLineHeight();

            if (SingleLine) {
                return lineHeight + PaddingTop + PaddingBottom;
            }
            float totalHeight = _lines.Count * lineHeight;
            return totalHeight + PaddingTop + PaddingBottom;
        }

        private float CalculateLineHeight() {
            return Font.LineSpacing * TextSize * LineSpacingMultiplier + LineSpacingExtra;
        }

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
            if (SingleLine) {
                string line = Text;
                if (TruncateAt != TruncateAt.None) {
                    line = TruncateText(Text, availableWidth / TextSize);
                }
                _lines.Add(line);
            } else {
                string[] paragraphs = Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string paragraph in paragraphs) {
                    if (_lines.Count >= MaxLines) break;

                    if (availableWidth == float.MaxValue) {
                        _lines.Add(paragraph);
                    } else {
                        WrapParagraph(paragraph, availableWidth / TextSize);
                    }
                }
            }
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
            if (_needsTextLayout) {
                LayoutText();
            }
            if (_isAnimating) {
                _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                int charsToShow = (int)(_elapsedTime * _charRevealSpeed);

                if (charsToShow >= Text.Length) {
                    charsToShow = Text.Length;
                    _isAnimating = false;
                }

                _displayedText = Text.Substring(0, charsToShow);
                _needsTextLayout = true;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible || Font == null) return;
            if (_needsTextLayout) {
                LayoutText();
            }

            if (_lines.Count == 0) return;

            string[] linesToDraw = _isAnimating ?
                WrapAnimatedText(_displayedText) :
                _lines.ToArray();

            float lineHeight = CalculateLineHeight();
            float totalTextHeight = linesToDraw.Length * lineHeight;
            float startY = Position.Y + PaddingTop;
            if ((Gravity & Gravity.Bottom) != 0) {
                startY = Position.Y + Height - totalTextHeight - PaddingBottom;
            } else if ((Gravity & Gravity.CenterVertical) != 0) {
                startY = Position.Y + (Height - totalTextHeight) / 2;
            }
            for (int i = 0; i < linesToDraw.Length; i++) {
                string line = linesToDraw[i];
                float lineWidth = Font.MeasureString(line).X * TextSize;
                float lineX = Position.X + PaddingLeft;
                if ((Gravity & Gravity.Right) != 0) {
                    lineX = Position.X + Width - lineWidth - PaddingRight;
                } else if ((Gravity & Gravity.CenterHorizontal) != 0) {
                    lineX = Position.X + (Width - lineWidth) / 2;
                } else if (TextAlign == TextAlignment.Center) {
                    lineX = Position.X + (Width - lineWidth) / 2;
                } else if (TextAlign == TextAlignment.ViewEnd) {
                    lineX = Position.X + Width - lineWidth - PaddingRight;
                }

                float lineY = startY + i * lineHeight;
                Vector2 linePosition = new Vector2(lineX, lineY);
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
                if (DrawOutline) {
                    DrawTextWithOutline(spriteBatch, Font, line, linePosition, TextColor, OutlineColor, OutlineWidth);
                } else {
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
            if (string.IsNullOrEmpty(animatedText)) {
                return new string[0];
            }

            float availableWidth = Width - PaddingLeft - PaddingRight;
            if (SingleLine || availableWidth <= 0) {
                return new[] { animatedText };
            }
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
            spriteBatch.DrawString(
                font,
                text,
                position,
                textColor * Opacity,
                0f, Vector2.Zero, TextSize,
                SpriteEffects.None, 0f
            );
        }
        public void RequestLayout() {
            _needsTextLayout = true;
        }
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

        public float GetTextWidth() {
            if (_needsTextLayout) LayoutText();
            return _contentWidth;
        }

        public float GetTextHeight() {
            if (_needsTextLayout) LayoutText();
            return _contentHeight;
        }

        public override Vector2 Size {
            get => base.Size;
            set {
                if (value != base.Size) {
                    base.Size = value;
                    _needsTextLayout = true;
                }
            }
        }

        public void AutoSize(bool horizontal = false, bool vertical = true) {
            if (_needsTextLayout) LayoutText();

            float newWidth = horizontal ? GetTextWidth() + PaddingLeft + PaddingRight : Width;
            float newHeight = vertical ? GetTextHeight() + PaddingTop + PaddingBottom : Height;

            Size = new Vector2(newWidth, newHeight);
        }
    }
}
