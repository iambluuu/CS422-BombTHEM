using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct3D;
using System;

namespace Client.Component {
    public class Button : IComponent {
        public Texture2D Texture { get; set; }
#nullable enable
        public Texture2D? Icon { get; set; }
        public string? Text { get; set; } = string.Empty;

        public SpriteFont? Font { get; set; } = FontHolder.Get("Font/DefaultFont");
        public ContentAlignment TextAlignment { get; set; } = ContentAlignment.MiddleCenter;
        public ContentAlignment IconAlignment { get; set; } = ContentAlignment.MiddleLeft;

        public int Padding { get; set; } = 5;

        public Color TextColor { get; set; } = Color.Black;
        public Vector2 TextSize { get; set; } = Vector2.Zero;
        public Color BackgroundColor { get; set; } = Color.White;

        public Button() {
            IsFocused = false;
            IsVisible = true;
            IsEnabled = true;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            if (Texture == null) {
                // Draw a default rectangle if no texture is provided
                Texture = new Texture2D(spriteBatch.GraphicsDevice, (int)Size.X, (int)Size.Y);
                Color[] data = new Color[(int)(Size.X * Size.Y)];
                for (int i = 0; i < data.Length; ++i) {
                    data[i] = BackgroundColor;
                }
                Texture.SetData(data);
            }

            spriteBatch.Draw(Texture, Position, BackgroundColor);

            if (Icon != null) {
                Vector2 iconPosition = GetAlignedPosition(IconAlignment, Icon.Bounds.Size.ToVector2(), Size);
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
                spriteBatch.DrawString(Font, Text, textPosition, TextColor, scale: scale, origin: Vector2.Zero, rotation: 0f, effects: SpriteEffects.None, layerDepth: 0f);
            }
        }

        public override void Update(GameTime gameTime) {
            if (!IsVisible) return;
        }

        public override void HandleInput(UIEvent uIEvent) {
            if (!IsEnabled) return;

            if (HitTest(Mouse.GetState().Position)) {
                if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                    OnMouseDown?.Invoke();
                } else if (Mouse.GetState().LeftButton == ButtonState.Released) {
                    OnMouseUp?.Invoke();
                    OnClick?.Invoke();
                } else {
                    OnMouseEnter?.Invoke();
                }
            } else {
                OnMouseLeave?.Invoke();
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