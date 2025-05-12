using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client {
    public class SpriteNode : SceneNode {
        
        protected Texture2D _texture;
        protected Vector2 _origin;
        protected Vector2 _size;

        public SpriteNode(Texture2D texture) {
            _texture = texture;
            _origin = new Vector2(0, 0);
            _size = new Vector2(texture.Width, texture.Height);
        }

        public SpriteNode(Texture2D texture, Vector2 size) {
            _texture = texture;
            _origin = new Vector2(0, 0);
            _size = size;
        }

        public void CenterOrigin() {
            _origin = new Vector2(_texture.Width / 2, _texture.Height / 2);
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            Vector2 position = Vector2.Transform(Vector2.Zero, transform);
            float rotation = RotationFromMatrix(transform);
            Vector2 scale = ScaleFromMatrix(transform);

            Vector2 textureScale = new Vector2(_size.X / _texture.Width, _size.Y / _texture.Height);

            spriteBatch.Draw(
                _texture,
                position,
                null,
                Color.White,
                rotation,
                _origin,
                scale * textureScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
