using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared;

namespace Client.Component {
    public class TextBox : IComponent {
        private readonly Vector2 CornerSize = new(6, 6);
        private readonly Rectangle TextureSize = new(0, 0, 16, 16);
        private const string TextureDir = "Texture/Theme/";

        public string Text { get; set; } = string.Empty;
        public string PlaceholderText { get; set; } = string.Empty;
        public int MaxLength { get; set; } = 100;
        public bool IsMultiline { get; set; } = false;
        public bool IsPassword { get; set; } = false;
        public bool IsReadOnly { get; set; } = false;
        public bool IsUppercase { get; set; } = false;
        public Color TextColor { get; set; } = Color.Black;
        public Color BackgroundColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.Black;
        public int BorderWidth { get; set; } = 1;
        public int Padding { get; set; } = 5;
        public float FontSize { get; set; } = 30f;

        private const int CaretBlinkRate = 1000; // milliseconds
        private double _caretBlinkTimer = 0;

        public ContentAlignment TextAlignment { get; set; } = ContentAlignment.MiddleLeft;
        public SpriteFont Font { get; set; } = FontHolder.Get("Font/PressStart2P");
        public Texture2D Texture { get; set; } = null!;

        public TextBox() {
            IsFocused = false;
            IsVisible = true;
            IsEnabled = true;
        }

        public override void HandleInput(UIEvent uiEvent) {
            base.HandleInput(uiEvent);
            if (IsReadOnly) return;

            if (uiEvent.Type == UIEventType.TextInput) {
                if (!char.IsControl(uiEvent.Character) && Text.Length < MaxLength) {
                    if (IsPassword) {
                        Text += '*';
                    } else if (IsUppercase) {
                        Text += char.ToUpper(uiEvent.Character);
                    } else {
                        Text += uiEvent.Character;
                    }
                }
            }

            if (uiEvent.Type == UIEventType.KeyPress && IsFocused) {
                if (uiEvent.Key == Keys.Back) {
                    if (Text.Length > 0) {
                        Text = Text[..^1];
                    }
                } else if (uiEvent.Key == Keys.Enter && !IsMultiline) {
                    // Handle Enter key for non-multiline text box
                    IsFocused = false;
                } else if (uiEvent.Key == Keys.Tab) {
                    // Handle Tab key for focus change
                    IsFocused = false;
                }
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            if (IsFocused) {
                _caretBlinkTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
                if (_caretBlinkTimer >= CaretBlinkRate) {
                    _caretBlinkTimer = 0;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            // Draw background
            Texture2D texture = GetTexture();
            if (texture != null) {
                DrawNineSlice(spriteBatch, texture);
            } else {
                texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                texture.SetData(new[] { Color.White });
                spriteBatch.Draw(texture, Position, Color.White);
            }

            // Draw border
            if (BorderWidth > 0) {
                var borderTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                borderTexture.SetData(new[] { BorderColor });

                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, BorderWidth), BorderColor); // Top border
                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)(Position.Y + Size.Y - BorderWidth), (int)Size.X, BorderWidth), BorderColor); // Bottom border
                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)Position.Y, BorderWidth, (int)Size.Y), BorderColor); // Left border
                spriteBatch.Draw(borderTexture, new Rectangle((int)(Position.X + Size.X - BorderWidth), (int)Position.Y, BorderWidth, (int)Size.Y), BorderColor); // Right border
            }

            // Draw text
            string rawText = string.IsNullOrEmpty(Text) && (!IsFocused || IsReadOnly) ? PlaceholderText : Text;
            Color colorToDraw = string.IsNullOrEmpty(Text) && !IsFocused ? TextColor * 0.5f : TextColor;
            float scale = ToPixels(FontSize) / Font.LineSpacing;
            string textToDraw = TruncateText(rawText, Font, Size.X - Padding * 2, scale);
            Vector2 textSize = string.IsNullOrEmpty(textToDraw) ? new(0, Font.LineSpacing * scale) : Font.MeasureString(textToDraw) * scale;

            if (IsPassword) {
                textToDraw = new string('*', Text.Length);
            }

            Vector2 textPosition = Position + new Vector2(Padding, Padding);
            if (TextAlignment == ContentAlignment.MiddleCenter) {
                textPosition.X += (Size.X - textSize.X) / 2;
                textPosition.Y += (Size.Y - textSize.Y) / 2;
            } else if (TextAlignment == ContentAlignment.MiddleRight) {
                textPosition.X += Size.X - textSize.X - Padding;
                textPosition.Y += (Size.Y - textSize.Y) / 2;
            } else if (TextAlignment == ContentAlignment.BottomLeft) {
                textPosition.X += Padding;
                textPosition.Y += Size.Y - textSize.Y - Padding;
            }

            spriteBatch.DrawString(Font, textToDraw, textPosition, colorToDraw, scale: scale, origin: Vector2.Zero, rotation: 0f, effects: SpriteEffects.None, layerDepth: 0f);

            // Draw caret
            if (!IsReadOnly && IsFocused && _caretBlinkTimer < CaretBlinkRate / 2) {
                Vector2 caretPosition = new(textPosition.X + textSize.X + Padding / 2, textPosition.Y);
                var blackTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                blackTexture.SetData(new[] { Color.Black });
                spriteBatch.Draw(blackTexture, new Rectangle((int)caretPosition.X, (int)caretPosition.Y, 5, (int)textSize.Y), TextColor);
            }
        }

        private static float ToPixels(float value) {
            IntPtr Handle = Client.Instance.Window.Handle;
            return Utils.ToPixels(value, Handle);
        }

        private static string TruncateText(string text, SpriteFont font, float maxWidth, float scale) {
            string ellipsis = "...";
            float ellipsisWidth = font.MeasureString(ellipsis).X * scale;

            if (font.MeasureString(text).X * scale <= maxWidth)
                return text;

            int index = text.Length - (int)((maxWidth - ellipsisWidth) / (font.MeasureString("A").X * scale));
            return index < 0 ? ellipsis : ellipsis + text.Substring(index);
        }

        private Texture2D GetTexture() {
            Texture2D texture = new Texture2D(TextureHolder.Get($"{TextureDir}nine_path_panel_3").GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.Red * 0.5f });

            try {
                texture = TextureHolder.Get($"{TextureDir}nine_path_panel_3", TextureSize);
            } catch (System.IO.FileNotFoundException ex) {
                Console.WriteLine($"Texture not found: {ex.Message}");
                return null;
            } catch (Exception ex) {
                Console.WriteLine($"Error loading texture: {ex.Message}");
                return null;
            }

            return texture;
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
    }
}