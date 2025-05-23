using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Client.ContentHolder {
    public static class FontHolder {
        private static readonly Dictionary<string, SpriteFont> _fonts = [];
        private static ContentManager _content;

        public static void SetContentManager(ContentManager content) {
            _content = content;
        }

        public static SpriteFont Get(string name) {
            if (_fonts.TryGetValue(name, out SpriteFont value)) {
                return value;
            } else {
                if (_content == null)
                    throw new System.Exception("ContentManager not set in FontHolder");

                SpriteFont font = _content.Load<SpriteFont>("Font/" + name);
                _fonts[name] = font;
                return font;
            }
        }

        public static void UnloadAll() {
            _fonts.Clear();
        }
    }
}
