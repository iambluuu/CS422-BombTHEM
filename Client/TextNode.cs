using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client {
    public class TextNode : SceneNode {
        private bool _centerOrigin;
        private string _text = string.Empty;
        private readonly SpriteFont _font = FontHolder.Get("PressStart2P");
        public float _textSize;
        private Color _textColor = Color.White;

        public TextNode(string text, float textSize = 1f, bool centerOrigin = false) {
            _text = text;
            _textSize = textSize;
            _centerOrigin = centerOrigin;
        }

        public void SetText(string text) {
            _text = text;
        }

        public void SetTextColor(Color color) {
            _textColor = color;
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);

            scale *= _textSize;

            if (_centerOrigin) {
                Vector2 textSize = _font.MeasureString(_text) * scale;
                position -= textSize / 2;
            }

            spriteBatch.DrawString(
                _font,
                _text,
                position,
                _textColor,
                rotation,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );
        }
    }

}