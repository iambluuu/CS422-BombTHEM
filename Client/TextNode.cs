using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client {
    public class TextNode : SceneNode {
        public string Text { get; set; }
        public SpriteFont Font { get; set; } = FontHolder.Get("PressStart2P");
        public Color Color { get; set; } = Color.White;

        public TextNode(string text) {
            Text = text;
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);

            spriteBatch.DrawString(
                Font,
                Text,
                position,
                Color,
                rotation,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );
        }
    }

}