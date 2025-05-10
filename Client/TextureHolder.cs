using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Client {
    public static class TextureHolder {
        private static readonly Dictionary<string, Texture2D> _textures = [];
        private static ContentManager _content;

        public static void SetContentManager(ContentManager content) {
            _content = content;
        }

        public static Texture2D Get(string name) {
            if (_textures.ContainsKey(name)) {
                return _textures[name];
            } else {
                if (_content == null)
                    throw new System.Exception("ContentManager not set in TextureHolder");

                Texture2D texture = _content.Load<Texture2D>($"Texture/{name}");
                _textures[name] = texture;
                return texture;
            }
        }

        public static Texture2D Get(string name, Rectangle cropArea) {
            string croppedKey = $"{name}_{cropArea.X}_{cropArea.Y}_{cropArea.Width}_{cropArea.Height}";

            if (_textures.ContainsKey(croppedKey)) {
                return _textures[croppedKey];
            } else {
                Texture2D sourceTexture = Get(name);
                Texture2D croppedTexture = Crop(sourceTexture, cropArea);

                _textures[croppedKey] = croppedTexture;
                return croppedTexture;
            }
        }

        public static void UnloadAll() {
            foreach (var texture in _textures.Values) {
                texture.Dispose();
            }
            _textures.Clear();
        }

        private static Texture2D Crop(Texture2D sourceTexture, Rectangle cropArea) {
            Texture2D croppedTexture = new(sourceTexture.GraphicsDevice, cropArea.Width, cropArea.Height);

            Color[] sourceData = new Color[sourceTexture.Width * sourceTexture.Height];
            sourceTexture.GetData(sourceData);

            Color[] croppedData = new Color[cropArea.Width * cropArea.Height];

            for (int y = 0; y < cropArea.Height; y++) {
                for (int x = 0; x < cropArea.Width; x++) {
                    int sourceIndex = (cropArea.Y + y) * sourceTexture.Width + (cropArea.X + x);
                    int targetIndex = y * cropArea.Width + x;

                    croppedData[targetIndex] = sourceData[sourceIndex];
                }
            }

            croppedTexture.SetData(croppedData);

            return croppedTexture;
        }
    }
}
