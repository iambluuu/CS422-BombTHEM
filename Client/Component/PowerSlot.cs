using Microsoft.VisualBasic.Devices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;

namespace Client.Component {
    public class PowerSlot : IComponent {
        private const int Paddings = 25; // surrounding padding
        private const int Spacing = 10; // spacing between entries

        // Nine-slice texture size
        private static readonly Vector2 CornerSize = new Vector2(6, 6);
        private static readonly Rectangle TextureSize = new(0, 0, 16, 16); // Assuming the texture is 16x16 pixels

        private Color _textColor = Color.White;

        private readonly Texture2D _backgroundTexture;
        private readonly Texture2D _borderTexture;
        private readonly SpriteFont _font;

        private (PowerName, int)[] _powers = [(PowerName.None, 0), (PowerName.None, 0)];

        public PowerSlot() {
            _backgroundTexture = TextureHolder.Get("Theme/nine_path_bg_2");
            _borderTexture = TextureHolder.Get("Theme/scoreboard_border");
            _font = FontHolder.Get("PressStart2P");
        }

        public void ObtainPower(PowerName power) {
            for (int i = 0; i < _powers.Length; i++) {
                if (_powers[i].Item1 == PowerName.None) {
                    _powers[i] = (power, GameplayConfig.PowerUpQuantity[power]);
                    break;
                }
            }
        }

        public void UsePower(char key) {
            int index = key switch {
                'Q' => 0,
                'E' => 1,
                _ => -1,
            };

            if (index < 0 || index >= _powers.Length) {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            // Console.WriteLine($"Using power: {_powers[index]}");

            if (_powers[index].Item1 != PowerName.None) {
                NetworkManager.Instance.Send(NetworkMessage.From(ClientMessageType.UsePowerUp, new() {
                    { "powerUpType", _powers[index].ToString() },
                }));
            }
        }

        public void PowerUpUsed(PowerName power) {
            for (int i = 0; i < _powers.Length; i++) {
                if (_powers[i].Item1 == power) {
                    _powers[i].Item2--;
                    if (_powers[i].Item2 == 0) {
                        _powers[i] = (PowerName.None, 0);
                    }
                    break;
                }
            }
        }


        public override void Update(GameTime gameTime) {

        }

        public override void Draw(SpriteBatch spriteBatch) {
            DrawNineSlice(spriteBatch, _backgroundTexture);
            DrawNineSlice(spriteBatch, _borderTexture);

            var textSize = _font.MeasureString("Power");
            var textScale = (Size.X - Paddings * 2) / textSize.X / 2;
            textSize *= textScale;

            var textPosition = new Vector2(Position.X + (Size.X - textSize.X) / 2, Position.Y + Paddings);
            spriteBatch.DrawString(_font, "Power", textPosition, _textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

            var slotSize = Math.Min((Size.X - Paddings * 2 - Spacing) / 2, Size.Y - Paddings * 2 - textSize.Y - Spacing);
            var leftMargin = (Size.X - slotSize * 2 - Spacing) / 2;
            for (int i = 0; i < _powers.Length; i++) {
                var power = _powers[i];
                var texture = GetPowerTexture(power.Item1);
                var scale = slotSize / texture.Width;
                var position = new Vector2(Position.X + leftMargin + (slotSize + Spacing) * i, Position.Y + Paddings + textSize.Y + Spacing);
                spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            // Draw Q and E keys
            var keySize = slotSize / 4;
            var charSize = _font.MeasureString("Q") * textScale;
            var charScale = (keySize - 6) / Math.Min(charSize.X, charSize.Y);
            var qPosition = new Vector2(Position.X + leftMargin, Position.Y + Paddings + textSize.Y + Spacing);
            var ePosition = new Vector2(Position.X + leftMargin + slotSize + Spacing, Position.Y + Paddings + textSize.Y + Spacing);
            var backgroundTexture = TextureHolder.Get("Theme/nine_path_bg_2");
            var backgroundScale = keySize / backgroundTexture.Width;

            spriteBatch.Draw(backgroundTexture, qPosition, null, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "Q", qPosition + new Vector2(3, 3), _textColor, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);

            if (_powers[0].Item2 > 1) {
                spriteBatch.Draw(backgroundTexture, qPosition + new Vector2(0, slotSize - backgroundTexture.Height * backgroundScale), null, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, _powers[0].Item2.ToString(), qPosition + new Vector2(3, slotSize - backgroundTexture.Height * backgroundScale + 3), Color.White, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);
            }

            ePosition += new Vector2(slotSize - keySize, 0);
            spriteBatch.Draw(backgroundTexture, ePosition, null, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, "E", ePosition + new Vector2(3, 3), _textColor, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);

            if (_powers[1].Item2 > 1) {
                spriteBatch.Draw(backgroundTexture, ePosition + new Vector2(0, slotSize - backgroundTexture.Height * backgroundScale), null, Color.White, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(_font, _powers[1].Item2.ToString(), ePosition + new Vector2(3, slotSize - backgroundTexture.Height * backgroundScale + 3), Color.White, 0f, Vector2.Zero, charScale, SpriteEffects.None, 0f);
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