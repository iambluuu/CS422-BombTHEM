using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Client.Component {
    public class Button : IComponent {
        public Texture2D Texture { get; set; }
#nullable enable
        public Texture2D? Icon { get; set; }
        public string? Text { get; set; } = string.Empty;

        public SpriteFont? Font { get; set; } = null;
        public ContentAlignment TextAlignment { get; set; } = ContentAlignment.MiddleCenter;
        public ContentAlignment IconAlignment { get; set; } = ContentAlignment.MiddleLeft;

        public int Padding { get; set; } = 5;

        public Color TextColor { get; set; } = Color.White;
        public Color BackgroundColor { get; set; } = Color.White;

        public Button(
            Texture2D texture,
            SpriteFont font,
            Vector2 position,
            Vector2 size,
            string? text = null,
            Texture2D? icon = null,
            ContentAlignment textAlignment = ContentAlignment.MiddleCenter,
            ContentAlignment iconAlignment = ContentAlignment.MiddleLeft,
            int padding = 8,
            Color? backgroundColor = null,
            Color? textColor = null
        ) {
            Texture = texture;
            Font = font;
            Position = position;
            Size = size;

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

            spriteBatch.Draw(Texture, Position, BackgroundColor);

            if (Icon != null) {
                Vector2 iconPosition = GetAlignedPosition(IconAlignment, Icon.Bounds.Size.ToVector2(), Size);
                spriteBatch.Draw(Icon, iconPosition, Color.White);
            }

            if (!string.IsNullOrEmpty(Text) && Font != null) {
                Vector2 textSize = Font.MeasureString(Text);
                Vector2 textPosition = GetAlignedPosition(TextAlignment, textSize, Size);
                spriteBatch.DrawString(Font, Text, textPosition, TextColor);
            }
        }

        public override void Update(GameTime gameTime) {
            if (!IsVisible) return;

            MouseState mouseState = Mouse.GetState();
            Point mousePos = new(mouseState.X, mouseState.Y);

            if (HitTest(mousePos)) {
                if (mouseState.LeftButton == ButtonState.Pressed) {
                    OnMouseDown?.Invoke();
                } else if (mouseState.LeftButton == ButtonState.Released) {
                    OnMouseUp?.Invoke();
                    OnClick?.Invoke();
                } else {
                    OnMouseOver?.Invoke();
                }
            } else {
                OnMouseOut?.Invoke();
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
    }
#nullable disable
}