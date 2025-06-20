using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TextCopy;

using Client.ContentHolder;

namespace Client.Component {
    [Flags]
    public enum CharacterSet {
        Alpha = 0x01,
        Numeric = 0x02,
        Whitespace = 0x04,
        NewLine = 0x08,
        Dot = 0x10,
        Slash = 0x20,
        Colon = 0x40,
        Underscore = 0x80,

        Alphanumeric = Alpha | Numeric,
        All = -1,
    }

    public class TextBox : IComponent {
        private readonly Vector2 CornerSize = new(6, 6);
        private readonly Rectangle TextureSize = new(0, 0, 16, 16);
        private const string TextureDir = "Theme/";

        private readonly List<char> _allowedCharacters = [];
        private CharacterSet _allowedCharactersSet = CharacterSet.All;
        public CharacterSet AllowedCharacters {
            private get => _allowedCharactersSet;
            set {
                _allowedCharactersSet = value;
                _allowedCharacters.Clear();
                if ((value & CharacterSet.Alpha) != 0) {
                    for (char c = 'A'; c <= 'Z'; c++) _allowedCharacters.Add(c);
                    for (char c = 'a'; c <= 'z'; c++) _allowedCharacters.Add(c);
                }

                if ((value & CharacterSet.Numeric) != 0) {
                    for (char c = '0'; c <= '9'; c++) _allowedCharacters.Add(c);
                }

                if ((value & CharacterSet.Whitespace) != 0) {
                    _allowedCharacters.Add(' ');
                }

                if ((value & CharacterSet.NewLine) != 0) {
                    _allowedCharacters.Add('\n');
                }

                if ((value & CharacterSet.Dot) != 0) {
                    _allowedCharacters.Add('.');
                }

                if ((value & CharacterSet.Slash) != 0) {
                    _allowedCharacters.Add('/');
                }

                if ((value & CharacterSet.Colon) != 0) {
                    _allowedCharacters.Add(':');
                }

                if ((value & CharacterSet.Underscore) != 0) {
                    _allowedCharacters.Add('_');
                }
            }
        }

        private string _text = string.Empty;
        private string InternalText {
            get => _text;
            set {
                string filteredText = filterText(value);

                if (_text != filteredText) {
                    _text = filteredText;
                    _needsTextLayout = true;
                }
            }
        }

        public string Text {
            get => InternalText;
            set {
                InternalText = value;
                _caretPosition = InternalText.Length;
            }
        }

        public string PlaceholderText { get; set; } = string.Empty;
        public int MaxLength { get; set; } = 100;
        public bool IsMultiline { get; set; } = false;
        public bool IsPassword { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;
        public readonly bool HasBackground = true;
        public bool IsUppercase { get; set; } = false;
        public TruncateAt TruncateAt { get; set; } = TruncateAt.None;
        public int MaxLines { get; set; } = int.MaxValue;
        public bool SingleLine {
            get => MaxLines == 1;
            set => MaxLines = value ? 1 : int.MaxValue;
        }
        public Color TextColor { get; set; } = Color.Black;
        public Color PlaceholderColor { get; set; } = Color.Gray;
        public Color BackgroundColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Black;
        public int BorderWidth { get; set; } = 0;

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
        public Gravity Gravity { get; set; } = Gravity.CenterLeft;
        private const int CaretBlinkRate = 1000;
        private double _caretBlinkTimer = 0;
        public int CaretWidth { get; set; } = 5;
        private int _caretPosition = 0;
        public SpriteFont Font { get; set; } = FontHolder.Get("PressStart2P");
        public Texture2D Texture { get; set; } = null!;
        private List<string> _lines = new List<string>();
        private bool _needsTextLayout = true;
        private float _contentWidth = 0;
        private float _contentHeight = 0;
        private List<int> _lineStartPositions = new List<int>();
        private const string ELLIPSIS = "...";

        private string filterText(string text) {
            if (_allowedCharactersSet == CharacterSet.All) {
                return text;
            }

            string filteredText = string.Empty;
            foreach (char c in text) {
                if (_allowedCharacters.Contains(c)) {
                    filteredText += c;
                }
            }

            return filteredText;
        }

        public override void HandleInput(UIEvent uiEvent) {
            base.HandleInput(uiEvent);
            if (IsReadOnly || !IsEnabled) return;

            if (uiEvent.Type == UIEventType.TextInput && IsFocused) {
                if (!char.IsControl(uiEvent.Character) && InternalText.Length < MaxLength && _allowedCharacters.Contains(uiEvent.Character)) {
                    if (IsUppercase) {
                        InternalText = InternalText.Insert(_caretPosition, char.ToUpper(uiEvent.Character).ToString());
                    } else {
                        InternalText = InternalText.Insert(_caretPosition, uiEvent.Character.ToString());
                    }
                    _caretPosition++;
                    _needsTextLayout = true;
                }
            }

            if (uiEvent.Type == UIEventType.KeyPress && IsFocused) {
                if (uiEvent.CtrlDown) {
                    if (uiEvent.Key == Keys.V) {
                        string clipboardText = ClipboardService.GetText();
                        foreach (char c in clipboardText) {
                            if (InternalText.Length < MaxLength && _allowedCharacters.Contains(c)) {
                                if (IsUppercase) {
                                    InternalText = InternalText.Insert(_caretPosition, char.ToUpper(c).ToString());
                                } else {
                                    InternalText = InternalText.Insert(_caretPosition, c.ToString());
                                }
                                _caretPosition++;
                            }
                        }
                        _needsTextLayout = true;
                    }
                } else if (uiEvent.Key == Keys.Back) {
                    if (InternalText.Length > 0 && _caretPosition > 0) {
                        InternalText = InternalText.Remove(_caretPosition - 1, 1);
                        _caretPosition--;
                        _needsTextLayout = true;
                    }
                } else if (uiEvent.Key == Keys.Delete) {
                    if (InternalText.Length > 0 && _caretPosition < InternalText.Length) {
                        InternalText = InternalText.Remove(_caretPosition, 1);
                        _needsTextLayout = true;
                    }
                } else if (uiEvent.Key == Keys.Left) {
                    if (_caretPosition > 0) {
                        _caretPosition--;
                    }
                } else if (uiEvent.Key == Keys.Right) {
                    if (_caretPosition < InternalText.Length) {
                        _caretPosition++;
                    }
                } else if (uiEvent.Key == Keys.Home) {
                    _caretPosition = 0;
                } else if (uiEvent.Key == Keys.End) {
                    _caretPosition = InternalText.Length;
                } else if (uiEvent.Key == Keys.Enter && IsMultiline && _allowedCharacters.Contains('\n')) {
                    if (InternalText.Length < MaxLength) {
                        InternalText = InternalText.Insert(_caretPosition, "\n");
                        _caretPosition++;
                        _needsTextLayout = true;
                    }
                } else if (uiEvent.Key == Keys.Enter && (!IsMultiline || !_allowedCharacters.Contains('\n'))) {
                    IsFocused = false;
                } else if (uiEvent.Key == Keys.Tab) {
                    IsFocused = false;
                }

                _caretBlinkTimer = 0;
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            if (_needsTextLayout) {
                LayoutText();
            }

            if (IsFocused) {
                _caretBlinkTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_caretBlinkTimer >= CaretBlinkRate) {
                    _caretBlinkTimer = 0;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;
            if (HasBackground) {
                Texture2D texture = GetTexture();
                if (texture != null) {
                    DrawNineSlice(spriteBatch, texture);
                } else {
                    texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                    texture.SetData(new[] { BackgroundColor });
                    spriteBatch.Draw(texture, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);
                }
            }
            if (BorderWidth > 0) {
                var borderTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                borderTexture.SetData(new[] { BorderColor });

                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, BorderWidth), BorderColor);
                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)(Position.Y + Size.Y - BorderWidth), (int)Size.X, BorderWidth), BorderColor);
                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)Position.Y, BorderWidth, (int)Size.Y), BorderColor);
                spriteBatch.Draw(borderTexture, new Rectangle((int)(Position.X + Size.X - BorderWidth), (int)Position.Y, BorderWidth, (int)Size.Y), BorderColor);
            }
            float lineHeight = CalculateLineHeight();
            if (string.IsNullOrEmpty(InternalText) && (!IsFocused || IsReadOnly) && !string.IsNullOrEmpty(PlaceholderText)) {
                Vector2 textPosition = CalculateTextPosition(Font.MeasureString(PlaceholderText) * TextSize);
                spriteBatch.DrawString(
                    Font,
                    PlaceholderText,
                    textPosition,
                    PlaceholderColor * Opacity,
                    0f, Vector2.Zero, TextSize,
                    SpriteEffects.None, 0f
                );
            } else if (_lines.Count > 0) {
                float startY = Position.Y + PaddingTop;
                float totalTextHeight = _lines.Count * lineHeight;
                if ((Gravity & Gravity.Bottom) != 0) {
                    startY = Position.Y + Size.Y - totalTextHeight - PaddingBottom;
                } else if ((Gravity & Gravity.CenterVertical) != 0) {
                    startY = Position.Y + (Size.Y - totalTextHeight) / 2;
                }
                int caretLine = 0;
                int caretLinePosition = 0;
                LocateCaretPosition(out caretLine, out caretLinePosition);
                for (int i = 0; i < _lines.Count; i++) {
                    string line = _lines[i];
                    string displayLine = IsPassword ? new string('*', line.Length) : line;
                    float lineWidth = Font.MeasureString(displayLine).X * TextSize;
                    float lineX = Position.X + PaddingLeft;

                    if ((Gravity & Gravity.Right) != 0) {
                        lineX = Position.X + Size.X - lineWidth - PaddingRight;
                    } else if ((Gravity & Gravity.CenterHorizontal) != 0) {
                        lineX = Position.X + (Size.X - lineWidth) / 2;
                    }

                    float lineY = startY + i * lineHeight;
                    Vector2 linePosition = new Vector2(lineX, lineY);
                    spriteBatch.DrawString(
                        Font,
                        displayLine,
                        linePosition,
                        TextColor * Opacity,
                        0f, Vector2.Zero, TextSize,
                        SpriteEffects.None, 0f
                    );
                    if (i == caretLine && IsFocused && !IsReadOnly && _caretBlinkTimer < CaretBlinkRate / 2) {
                        string textBeforeCaret = IsPassword
                            ? new string('*', caretLinePosition)
                            : displayLine.Substring(0, Math.Min(caretLinePosition, displayLine.Length));

                        Vector2 caretTextSize = Font.MeasureString(textBeforeCaret) * TextSize;
                        Vector2 caretPosition = new Vector2(
                            linePosition.X + caretTextSize.X,
                            linePosition.Y
                        );

                        var caretTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                        caretTexture.SetData(new[] { TextColor });
                        spriteBatch.Draw(
                            caretTexture,
                            new Rectangle(
                                (int)caretPosition.X,
                                (int)caretPosition.Y,
                                CaretWidth,
                                (int)(Font.LineSpacing * TextSize)
                            ),
                            TextColor * Opacity
                        );
                    }
                }
            }
        }
        private void LocateCaretPosition(out int line, out int positionInLine) {
            line = 0;
            positionInLine = 0;

            if (_lineStartPositions.Count == 0 || _caretPosition == 0) {
                return;
            }
            for (int i = 1; i < _lineStartPositions.Count; i++) {
                if (_caretPosition < _lineStartPositions[i]) {
                    line = i - 1;
                    positionInLine = _caretPosition - _lineStartPositions[line];
                    return;
                }
            }
            line = _lineStartPositions.Count - 1;
            positionInLine = _caretPosition - _lineStartPositions[line];
        }

        private Vector2 CalculateTextPosition(Vector2 textSize) {
            Vector2 position = Position + new Vector2(PaddingLeft, PaddingTop);
            float availableWidth = Size.X - PaddingLeft - PaddingRight;
            float availableHeight = Size.Y - PaddingTop - PaddingBottom;
            if ((Gravity & Gravity.Right) != 0) {
                position.X = Position.X + Size.X - textSize.X - PaddingRight;
            } else if ((Gravity & Gravity.CenterHorizontal) != 0) {
                position.X = Position.X + (Size.X - textSize.X) / 2;
            }
            if ((Gravity & Gravity.Bottom) != 0) {
                position.Y = Position.Y + Size.Y - textSize.Y - PaddingBottom;
            } else if ((Gravity & Gravity.CenterVertical) != 0) {
                position.Y = Position.Y + (Size.Y - textSize.Y) / 2;
            }

            return position;
        }
        protected override float MeasureContentWidth() {
            if (string.IsNullOrEmpty(InternalText) && string.IsNullOrEmpty(PlaceholderText))
                return PaddingLeft + PaddingRight;

            if (_needsTextLayout) LayoutText();
            return _contentWidth + PaddingLeft + PaddingRight;
        }
        protected override float MeasureContentHeight() {
            if (_needsTextLayout) LayoutText();

            float lineHeight = CalculateLineHeight();

            if (SingleLine || _lines.Count == 0) {
                return lineHeight + PaddingTop + PaddingBottom;
            }
            return _lines.Count * lineHeight + PaddingTop + PaddingBottom;
        }

        private float CalculateLineHeight() {
            return Font.LineSpacing * TextSize * LineSpacingMultiplier + LineSpacingExtra;
        }
        private void LayoutText() {
            _lines.Clear();
            _lineStartPositions.Clear();
            _lineStartPositions.Add(0);

            if (string.IsNullOrEmpty(InternalText) && string.IsNullOrEmpty(PlaceholderText)) {
                _lines.Add(string.Empty);
                _contentWidth = 0;
                _contentHeight = 0;
                _needsTextLayout = false;
                return;
            }

            float availableWidth = Size.X - PaddingLeft - PaddingRight;
            if (availableWidth <= 0) availableWidth = float.MaxValue;
            if (IsMultiline && TruncateAt == TruncateAt.None && !SingleLine) {
                string[] paragraphs = InternalText.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
                int runningPosition = 0;

                foreach (string paragraph in paragraphs) {
                    if (_lines.Count >= MaxLines) break;

                    if (string.IsNullOrEmpty(paragraph)) {
                        _lines.Add(string.Empty);
                        runningPosition++;
                        if (_lines.Count < MaxLines) {
                            _lineStartPositions.Add(runningPosition);
                        }
                    } else if (availableWidth == float.MaxValue) {
                        _lines.Add(paragraph);
                        runningPosition += paragraph.Length + 1;
                        if (_lines.Count < MaxLines) {
                            _lineStartPositions.Add(runningPosition);
                        }
                    } else {
                        int paraStartPos = runningPosition;
                        WrapParagraph(paragraph, availableWidth / TextSize, paraStartPos, ref runningPosition);
                    }
                }
            } else {
                if (SingleLine || !IsMultiline) {
                    string line = InternalText;
                    if (TruncateAt != TruncateAt.None) {
                        line = TruncateText(InternalText, availableWidth / TextSize);
                    }
                    _lines.Add(line);
                } else {
                    string[] paragraphs = InternalText.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
                    int runningPosition = 0;

                    foreach (string paragraph in paragraphs) {
                        if (_lines.Count >= MaxLines) break;

                        _lines.Add(paragraph);
                        runningPosition += paragraph.Length + 1;
                        if (_lines.Count < MaxLines) {
                            _lineStartPositions.Add(runningPosition);
                        }
                    }
                    if (TruncateAt != TruncateAt.None) {
                        for (int i = 0; i < _lines.Count; i++) {
                            _lines[i] = TruncateText(_lines[i], availableWidth / TextSize);
                        }
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

        private void WrapParagraph(string paragraph, float maxLineWidth, int startPos, ref int runningPosition) {
            if (string.IsNullOrEmpty(paragraph)) {
                _lines.Add(string.Empty);
                runningPosition++;
                if (_lines.Count < MaxLines) {
                    _lineStartPositions.Add(runningPosition);
                }
                return;
            }

            string[] words = paragraph.Split(' ');
            string currentLine = string.Empty;
            int currentLineStartPos = startPos;

            foreach (string word in words) {
                string testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
                Vector2 size = Font.MeasureString(testLine);

                if (size.X <= maxLineWidth || currentLine.Length == 0) {
                    currentLine = testLine;
                } else {
                    _lines.Add(currentLine);
                    int lineLength = currentLine.Length;
                    runningPosition = currentLineStartPos + lineLength + 1;

                    if (_lines.Count >= MaxLines) {
                        if (TruncateAt == TruncateAt.End && _lines.Count > 0) {
                            string lastLine = _lines[_lines.Count - 1];
                            _lines[_lines.Count - 1] = TruncateText(lastLine, maxLineWidth);
                        }
                        return;
                    }

                    _lineStartPositions.Add(runningPosition);
                    currentLineStartPos = runningPosition;
                    currentLine = word;
                }
            }

            if (currentLine.Length > 0) {
                _lines.Add(currentLine);
                runningPosition = currentLineStartPos + currentLine.Length + 1;
                if (_lines.Count < MaxLines) {
                    _lineStartPositions.Add(runningPosition);
                }
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
                string endPart = text.Substring(text.Length - (length - halfLength));
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

        private Texture2D GetTexture() {
            try {
                return TextureHolder.Get($"{TextureDir}nine_path_panel_3", TextureSize);
            } catch (Exception ex) {
                Console.WriteLine($"Error loading texture: {ex.Message}");
                return null;
            }
        }

        private void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture) {
            var scale = 4;
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

        public void RequestLayout() {
            _needsTextLayout = true;
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
