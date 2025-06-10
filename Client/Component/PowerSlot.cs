using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

using Shared;
using Client.Scene;
using Client.ContentHolder;

namespace Client.Component {
    public class PowerSlot : IComponent {
        private const int Paddings = 25; // surrounding padding
        private const int Spacing = 10; // spacing between entries
        private readonly MapRenderInfo map;

        // Nine-slice texture size
        private static readonly Vector2 CornerSize = new Vector2(6, 6);
        private static readonly Rectangle TextureSize = new(0, 0, 16, 16); // Assuming the texture is 16x16 pixels

        private Color _textColor = Color.White;

        private readonly Texture2D _backgroundTexture;
        private readonly Texture2D _borderTexture;
        private readonly Texture2D _glowTexture;
        private float _glowScale = 0;
        private float _slotSize;
        private float _leftMargin;
        private float _textScale;
        private Vector2 _textSize;

        private readonly SpriteFont _font;

        public override Vector2 Size {
            get => base.Size;
            set {
                base.Size = value;
                CalculateValues();
            }
        }

        public override SizeMode WidthMode {
            get => base.WidthMode;
            set {
                base.WidthMode = value;
                CalculateValues();
            }
        }

        public override SizeMode HeightMode {
            get => base.HeightMode;
            set {
                base.HeightMode = value;
                CalculateValues();
            }
        }

        public PowerSlot(MapRenderInfo map) {
            _backgroundTexture = TextureHolder.Get("Theme/nine_path_bg_2");
            _borderTexture = TextureHolder.Get("Theme/scoreboard_border");
            _font = FontHolder.Get("PressStart2P");
            _textSize = _font.MeasureString("Power");

            CalculateValues();

            var tmp = TextureHolder.Get("Power/None");
            Color[] data = new Color[tmp.Width * tmp.Height];
            tmp.GetData(data);
            for (int j = 0; j < data.Length; j++) {
                if (data[j].A == 0)
                    continue;
                data[j] = Color.White;
            }
            _glowTexture = new Texture2D(tmp.GraphicsDevice, tmp.Width, tmp.Height);
            _glowTexture.SetData(data);
            this.map = map;
        }

        private void CalculateValues() {
            _textScale = (Size.X - Paddings * 2) / _textSize.X / 2;
            _textSize *= _textScale;
            _slotSize = Math.Min((Size.X - Paddings * 2 - Spacing) / 2, Size.Y - Paddings * 2 - _textSize.Y - Spacing);
            _leftMargin = (Size.X - _slotSize * 2 - Spacing) / 2;
        }

        public override void Update(GameTime gameTime) {
            if (map == null || !map.IsInitialized) {
                return;
            }

            _glowScale = ((float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5) + 1) * 0.5f;
            CalculateValues();
        }

        public override void Draw(SpriteBatch spriteBatch) {
            DrawNineSlice(spriteBatch, _backgroundTexture);
            DrawNineSlice(spriteBatch, _borderTexture);

            // Draw the "Power" text
            var textPosition = new Vector2(Position.X + (Size.X - _textSize.X) / 2, Position.Y + Paddings);
            spriteBatch.DrawString(_font, "Power", textPosition, _textColor, 0f, Vector2.Zero, _textScale, SpriteEffects.None, 0f);

            // Draw the power icons
            var powers = map.PowerUps;
            for (int i = 0; i < powers.Length; i++) {
                var power = powers[i].Item1;
                var texture = GetPowerTexture(power);
                var scale = _slotSize / texture.Width;
                var position = new Vector2(Position.X + _leftMargin + (_slotSize + Spacing) * i, Position.Y + Paddings + _textSize.Y + Spacing);

                if (powers[i].Item3) {
                    float glowScale = _glowScale * 0.05f + 1.1f;
                    float sizeScale = _slotSize / _glowTexture.Width;
                    Vector2 positionOffset = new Vector2(-_slotSize * (glowScale - 1) / 2, -_slotSize * (glowScale - 1) / 2);
                    spriteBatch.Draw(_glowTexture, position + positionOffset, null, Color.White, 0f, Vector2.Zero, glowScale * sizeScale, SpriteEffects.None, 0f);
                }

                spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            // Draw Q and E keys overlay
            var keySize = _slotSize / 4;
            var charSize = _font.MeasureString("Q") * _textScale;
            var charScale = (keySize - 6) / Math.Min(charSize.X, charSize.Y);
            var qPosition = new Vector2(Position.X + _leftMargin, Position.Y + Paddings + _textSize.Y + Spacing);
            var ePosition = new Vector2(Position.X + _leftMargin + _slotSize + Spacing, Position.Y + Paddings + _textSize.Y + Spacing);
            var backgroundTexture = TextureHolder.Get("Theme/nine_path_bg_2");
            var backgroundScale = keySize / backgroundTexture.Width;

            spriteBatch.Draw(backgroundTexture, qPosition, null, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "Q", qPosition + new Vector2(3, 3), _textColor, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);

            // Only show quantity if the slot is activated
            if (powers[0].Item3) {
                spriteBatch.Draw(backgroundTexture, qPosition + new Vector2(0, _slotSize - backgroundTexture.Height * backgroundScale), null, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, powers[0].Item2.ToString(), qPosition + new Vector2(3, _slotSize - backgroundTexture.Height * backgroundScale + 3), Color.White, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);
            }

            ePosition += new Vector2(_slotSize - keySize, 0);
            spriteBatch.Draw(backgroundTexture, ePosition, null, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "E", ePosition + new Vector2(3, 3), _textColor, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);

            if (powers[1].Item3) {
                spriteBatch.Draw(backgroundTexture, ePosition + new Vector2(0, _slotSize - backgroundTexture.Height * backgroundScale), null, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, powers[1].Item2.ToString(), ePosition + new Vector2(3, _slotSize - backgroundTexture.Height * backgroundScale + 3), Color.White, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);
            }
        }

        private Texture2D GetPowerTexture(PowerName power) {
            return TextureHolder.Get($"Power/{power}");
        }

        private void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture) {
            var scale = (Paddings - 5) / CornerSize.X; // Scale factor based on padding and corner size
            var scaledCornerSize = CornerSize * scale;

            Rectangle srcTopLeft = new(0, 0, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcTop = new((int)CornerSize.X, 0, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);
            Rectangle srcTopRight = new(TextureSize.Width - (int)CornerSize.X, 0, (int)CornerSize.X, (int)CornerSize.Y);

            Rectangle srcBottomLeft = new(0, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottomRight = new(TextureSize.Width - (int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottom = new((int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);

            Rectangle srcMiddle = new((int)CornerSize.X, (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleLeft = new(0, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleRight = new(TextureSize.Width - (int)CornerSize.X, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));

            Rectangle dstTopLeft = new((int)Position.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstTop = new((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);
            Rectangle dstTopRight = new((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);

            Rectangle dstBottomLeft = new((int)Position.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottomRight = new((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottom = new((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);

            Rectangle dstMiddle = new((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleLeft = new((int)Position.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleRight = new((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));

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
}