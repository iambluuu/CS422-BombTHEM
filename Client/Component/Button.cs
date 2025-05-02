using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct3D;
using System;

namespace Client.Component {
    enum StateOfButton {
        Normal,
        Hovered,
        Pressed,
        Disabled
    }

    public class Button : IComponent {
        // public Texture2D Texture { get; set; }
#nullable enable
        private readonly Vector2 CornerSize = new(3, 3);
        private readonly Rectangle TextureSize = new(0, 0, 16, 8);
        private const int PressedOffset = 5;
        private const string TextureDir = "Texture/Theme/";

        public Texture2D? Icon { get; set; }
        public string? Text { get; set; } = string.Empty;

        public SpriteFont? Font { get; set; } = null!;
        public ContentAlignment TextAlignment { get; set; } = ContentAlignment.MiddleCenter;
        public ContentAlignment IconAlignment { get; set; } = ContentAlignment.MiddleLeft;

        public int Padding { get; set; } = 5;

        public Color TextColor { get; set; } = Color.White;
        public Vector2 TextSize { get; set; } = Vector2.Zero;
        public Color BackgroundColor { get; set; } = Color.White;

        private StateOfButton _state = StateOfButton.Normal;

        public Button(
            Vector2? position,
            Vector2? size,
            Action? onClick = null,
            Texture2D? texture = null,
            SpriteFont? font = default,
            string? text = null,
            Texture2D? icon = null,
            ContentAlignment textAlignment = ContentAlignment.MiddleCenter,
            ContentAlignment iconAlignment = ContentAlignment.MiddleLeft,
            int padding = 8,
            Color? backgroundColor = null,
            Color? textColor = null
        ) {
            // Texture = texture;
            Font = font;
            Position = position ?? Vector2.Zero;
            Size = size ?? new Vector2(100, 50);

            OnClick = onClick;

            Text = text;
            Icon = icon;

            TextAlignment = textAlignment;
            IconAlignment = iconAlignment;
            Padding = padding;

            BackgroundColor = backgroundColor ?? Color.White;
            TextColor = textColor ?? Color.Black;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            var texture = GetTexture();
            if (texture.Width == 1 && texture.Height == 1) {
                spriteBatch.Draw(texture, Position, BackgroundColor);
            } else {
                DrawNineSlice(spriteBatch, texture);
            }

            if (Icon != null) {
                Vector2 iconPosition = GetAlignedPosition(IconAlignment, Icon.Bounds.Size.ToVector2(), Size);
                if (_state == StateOfButton.Pressed) {
                    iconPosition.Y += PressedOffset;
                }
                spriteBatch.Draw(Icon, iconPosition, Color.White);
            }

            if (!string.IsNullOrEmpty(Text) && Font != null) {
                Vector2 textSize = Font.MeasureString(Text);
                if (TextSize == Vector2.Zero) {
                    TextSize = textSize;
                }
                float scale = Math.Min((Size.X - Padding * 2) / textSize.X, (Size.Y - Padding * 2) / textSize.Y);
                scale = Math.Clamp(scale, 0.2f, 1f); // Ensure scale is within a reasonable range
                textSize *= scale;
                // Adjust text size based on icon size and padding
                Vector2 textPosition = GetAlignedPosition(TextAlignment, textSize, Size);
                if (_state == StateOfButton.Pressed) {
                    textPosition.Y += PressedOffset;
                }
                spriteBatch.DrawString(Font, Text, textPosition, TextColor, scale: scale, origin: Vector2.Zero, rotation: 0f, effects: SpriteEffects.None, layerDepth: 0f);
            }
        }

        public override void Update(GameTime gameTime) {
            if (!IsVisible) return;
        }

        public override void HandleInput(UIEvent uiEvent) {
            if (!IsEnabled) return;

            if (HitTest(Mouse.GetState().Position)) {
                if (uiEvent.Type == UIEventType.MouseDown) {
                    OnMouseDown?.Invoke();
                    _state = StateOfButton.Pressed;
                } else if (uiEvent.Type == UIEventType.MouseUp && _state == StateOfButton.Pressed) {
                    OnMouseUp?.Invoke();
                    OnClick?.Invoke();
                    _state = StateOfButton.Hovered;
                } else if (_state != StateOfButton.Pressed) {
                    OnMouseEnter?.Invoke();
                    _state = StateOfButton.Hovered;
                }
            } else {
                OnMouseLeave?.Invoke();
                _state = StateOfButton.Normal;
            }
        }

        private Vector2 GetAlignedPosition(ContentAlignment alignment, Vector2 contentSize, Vector2 containerSize) {
            Vector2 position = Position;

            switch (alignment) {
                case ContentAlignment.TopLeft:
                    position += new Vector2(Padding, Padding);
                    break;
                case ContentAlignment.TopCenter:
                    position += new Vector2((containerSize.X - contentSize.X) / 2, Padding);
                    break;
                case ContentAlignment.TopRight:
                    position += new Vector2(containerSize.X - contentSize.X - Padding, Padding);
                    break;
                case ContentAlignment.MiddleLeft:
                    position += new Vector2(Padding, (containerSize.Y - contentSize.Y) / 2);
                    break;
                case ContentAlignment.MiddleCenter:
                    position += new Vector2((containerSize.X - contentSize.X) / 2, (containerSize.Y - contentSize.Y) / 2);
                    break;
                case ContentAlignment.MiddleRight:
                    position += new Vector2(containerSize.X - contentSize.X - Padding, (containerSize.Y - contentSize.Y) / 2);
                    break;
                case ContentAlignment.BottomLeft:
                    position += new Vector2(Padding, containerSize.Y - contentSize.Y - Padding);
                    break;
                case ContentAlignment.BottomCenter:
                    position += new Vector2((containerSize.X - contentSize.X) / 2, containerSize.Y - contentSize.Y - Padding);
                    break;
                case ContentAlignment.BottomRight:
                    position += new Vector2(containerSize.X - contentSize.X - Padding, containerSize.Y - contentSize.Y - Padding);
                    break;
            }

            return position;
        }

        private Texture2D GetTexture() {
            Texture2D texture = new Texture2D(TextureHolder.Get($"{TextureDir}button_normal").GraphicsDevice, 1, 1);
            texture.SetData(new[] { BackgroundColor });

            try {
                switch (_state) {
                    case StateOfButton.Normal:
                        texture = TextureHolder.Get($"{TextureDir}button_normal");
                        break;
                    case StateOfButton.Hovered:
                        texture = TextureHolder.Get($"{TextureDir}button_hover");
                        break;
                    case StateOfButton.Pressed:
                        texture = TextureHolder.Get($"{TextureDir}button_pressed");
                        break;
                    case StateOfButton.Disabled:
                        texture = TextureHolder.Get($"{TextureDir}button_disabled");
                        break;
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error loading texture: {ex.Message}");
            }

            return texture;
        }

        private void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture) {
            var scale = 7;
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
#nullable disable
}