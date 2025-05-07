using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Component {
    public enum ScaleMode {
        Stretch,
        Fit,
        Fill,
        Crop
    }

    public class ImageComponent : IComponent {
        public Texture2D Texture { get; set; }
        public ScaleMode ScaleMode { get; set; }
        public Color Tint { get; set; } = Color.White;

        public ImageComponent(Texture2D texture, ScaleMode scaleMode = ScaleMode.Stretch, Vector2? size = null) {
            Texture = texture;
            ScaleMode = scaleMode;
            Size = size ?? new Vector2(texture.Width, texture.Height);
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            // No specific update logic for ImageComponent
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (Texture == null || !IsVisible) return;

            var sourceRectangle = new Rectangle(0, 0, Texture.Width, Texture.Height);
            var destinationRectangle = new Rectangle(0, 0, 10, 10); // Placeholder for destination rectangle
            switch (ScaleMode) {
                case ScaleMode.Stretch:
                    destinationRectangle = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
                    break;
                case ScaleMode.Fit:
                    float aspectRatio = (float)Texture.Width / Texture.Height;
                    if (Size.X / Size.Y > aspectRatio) {
                        // Fit by height
                        var height = Size.Y;
                        var width = height * aspectRatio;
                        destinationRectangle = new Rectangle((int)(Position.X + (Size.X - width) / 2), (int)Position.Y, (int)(Size.Y * aspectRatio), (int)Size.Y);
                    } else {
                        // Fit by width
                        var width = Size.X;
                        var height = width / aspectRatio;
                        destinationRectangle = new Rectangle((int)Position.X, (int)(Position.Y + (Size.Y - height) / 2), (int)Size.X, (int)(Size.X / aspectRatio));
                    }
                    break;
                case ScaleMode.Fill:
                    // Similar to Fit but fills the entire area
                    destinationRectangle = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
                    break;
                case ScaleMode.Crop:
                    // Crop logic can be implemented here
                    break;
            }

            spriteBatch.Draw(Texture, destinationRectangle, sourceRectangle, Tint * Opacity, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }
    }
}