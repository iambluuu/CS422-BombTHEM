using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Component {
    public class ContainerBox : LinearLayout {
        private readonly Vector2 CornerSize = new(6, 6);
        private readonly Rectangle TextureSize = new(0, 0, 16, 16);
        private const string TextureDir = "Theme/";

        // Visual properties
        public Color BackgroundColor { get; set; } = Color.White;
        public string TextureName { get; set; } = "nine_path_panel_2";
        public int TextureScale { get; set; } = 5;
        public bool DrawBorder { get; set; } = false;
        public int BorderWidth { get; set; } = 1;
        public Color BorderColor { get; set; } = Color.Black;

        public ContainerBox() {
            Padding = 50;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            // Draw background first
            Texture2D texture = GetTexture();
            if (texture != null) {
                DrawNineSlice(spriteBatch, texture);
            } else {
                // Fallback to solid color
                texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                texture.SetData(new[] { BackgroundColor });
                spriteBatch.Draw(texture, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor * Opacity);
            }

            // Draw border if needed
            if (DrawBorder && BorderWidth > 0) {
                var borderTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                borderTexture.SetData(new[] { BorderColor });

                // Top border
                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, BorderWidth), BorderColor * Opacity);
                // Bottom border
                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)(Position.Y + Size.Y - BorderWidth), (int)Size.X, BorderWidth), BorderColor * Opacity);
                // Left border
                spriteBatch.Draw(borderTexture, new Rectangle((int)Position.X, (int)Position.Y, BorderWidth, (int)Size.Y), BorderColor * Opacity);
                // Right border
                spriteBatch.Draw(borderTexture, new Rectangle((int)(Position.X + Size.X - BorderWidth), (int)Position.Y, BorderWidth, (int)Size.Y), BorderColor * Opacity);
            }

            // Then draw all components (using the base class implementation)
            base.Draw(spriteBatch);
        }

        private Texture2D GetTexture() {
            try {
                return TextureHolder.Get($"{TextureDir}{TextureName}", TextureSize);
            } catch (System.IO.FileNotFoundException ex) {
                Console.WriteLine($"Texture not found: {ex.Message}");
                return null;
            } catch (Exception ex) {
                Console.WriteLine($"Error loading texture: {ex.Message}");
                return null;
            }
        }

        private void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture) {
            var scaledCornerSize = CornerSize * TextureScale;

            // Source rectangles
            Rectangle srcTopLeft = new Rectangle(0, 0, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcTop = new Rectangle((int)CornerSize.X, 0, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);
            Rectangle srcTopRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, 0, (int)CornerSize.X, (int)CornerSize.Y);

            Rectangle srcBottomLeft = new Rectangle(0, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottomRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottom = new Rectangle((int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);

            Rectangle srcMiddle = new Rectangle((int)CornerSize.X, (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleLeft = new Rectangle(0, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));

            // Destination rectangles
            Rectangle dstTopLeft = new Rectangle((int)Position.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstTop = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);
            Rectangle dstTopRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);

            Rectangle dstBottomLeft = new Rectangle((int)Position.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottomRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottom = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);

            Rectangle dstMiddle = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleLeft = new Rectangle((int)Position.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));

            // Draw all nine slices with proper opacity
            Color colorTint = Color.White * Opacity;

            spriteBatch.Draw(texture, dstTopLeft, srcTopLeft, colorTint);
            spriteBatch.Draw(texture, dstTop, srcTop, colorTint);
            spriteBatch.Draw(texture, dstTopRight, srcTopRight, colorTint);

            spriteBatch.Draw(texture, dstBottomLeft, srcBottomLeft, colorTint);
            spriteBatch.Draw(texture, dstBottom, srcBottom, colorTint);
            spriteBatch.Draw(texture, dstBottomRight, srcBottomRight, colorTint);

            spriteBatch.Draw(texture, dstMiddle, srcMiddle, colorTint);
            spriteBatch.Draw(texture, dstMiddleLeft, srcMiddleLeft, colorTint);
            spriteBatch.Draw(texture, dstMiddleRight, srcMiddleRight, colorTint);
        }
    }
}