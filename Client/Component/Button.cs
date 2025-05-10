using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Client.Component {
    public enum StateOfButton {
        Normal,
        Hovered,
        Pressed,
        Disabled
    }

    public class Button : IComponent {
        // Texture constants
        private readonly Vector2 CornerSize = new(3, 3);
        private readonly Rectangle TextureSize = new(0, 0, 16, 8);
        private const int PressedOffset = 5;
        private const string TextureDir = "Texture/Theme/";

        // Content properties
        public Texture2D Icon { get; set; }
        private string _text = string.Empty;
        public string Text {
            get => _text;
            set {
                if (_text != value) {
                    _text = value;
                    _needsTextMeasurement = true;
                }
            }
        }

        // Text appearance properties
        private SpriteFont _font = FontHolder.Get("Font/PressStart2P");
        public SpriteFont Font {
            get => _font;
            set {
                if (_font != value) {
                    _font = value;
                    _needsTextMeasurement = true;
                }
            }
        }

        private float _textSize = 1.0f;
        public float TextSize {
            get => _textSize;
            set {
                if (_textSize != value) {
                    _textSize = value;
                    _needsTextMeasurement = true;
                }
            }
        }

        public Color TextColor { get; set; } = Color.White;

        // Icon properties
        public Color IconColor { get; set; } = Color.White;
        public float IconScale { get; set; } = 1.0f;

        // Layout properties
        public Gravity Gravity { get; set; } = Gravity.Center;
        public int ContentSpacing { get; set; } = 10; // Space between icon and text

        // Style properties
        public Color BackgroundColor { get; set; } = Color.White;

        // State
        private StateOfButton _state = StateOfButton.Normal;
        private bool _needsTextMeasurement = true;
        private Vector2 _measuredTextSize = Vector2.Zero;

        // Text effects
        public bool DrawTextShadow { get; set; } = false;
        public Vector2 TextShadowOffset { get; set; } = new Vector2(1, 1);
        public Color TextShadowColor { get; set; } = new Color(0, 0, 0, 128);

        // Text outline effect
        public bool DrawTextOutline { get; set; } = false;
        public int TextOutlineWidth { get; set; } = 1;
        public Color TextOutlineColor { get; set; } = Color.Black;

        // Ripple effect (Android-like touch feedback)
        private bool _showRipple = false;
        private Vector2 _rippleCenter;
        private float _rippleRadius = 0;
        private float _maxRippleRadius;
        private Color _rippleColor = new Color(255, 255, 255, 40);
        public bool EnableRippleEffect { get; set; } = false;

        // Elevation/Shadow
        public bool DrawButtonShadow { get; set; } = false;
        public int ShadowSize { get; set; } = 4;
        public Color ShadowColor { get; set; } = new Color(0, 0, 0, 60);

        public Button() {
            IsFocused = false;
            IsVisible = true;
            IsEnabled = true;
        }

        // Override MeasureContentWidth for SizeMode.WrapContent
        protected override float MeasureContentWidth() {
            MeasureTextIfNeeded();

            float contentWidth = Padding * 2; // Start with padding

            // Add icon width if present
            if (Icon != null) {
                contentWidth += Icon.Width * IconScale;

                // Add spacing between icon and text if both exist
                if (!string.IsNullOrEmpty(Text)) {
                    contentWidth += ContentSpacing;
                }
            }

            // Add text width if present
            if (!string.IsNullOrEmpty(Text)) {
                contentWidth += _measuredTextSize.X;
            }

            return contentWidth;
        }

        // Override MeasureContentHeight for SizeMode.WrapContent
        protected override float MeasureContentHeight() {
            MeasureTextIfNeeded();

            float iconHeight = (Icon != null) ? Icon.Height * IconScale : 0;
            float textHeight = (!string.IsNullOrEmpty(Text)) ? _measuredTextSize.Y : 0;

            // Use the taller content height + padding
            return Math.Max(iconHeight, textHeight) + (Padding * 2);
        }

        private void MeasureTextIfNeeded() {
            if (_needsTextMeasurement && !string.IsNullOrEmpty(Text) && Font != null) {
                _measuredTextSize = Font.MeasureString(Text) * TextSize;
                _needsTextMeasurement = false;
            }
        }

        public override void Update(GameTime gameTime) {
            if (!IsVisible) return;

            base.Update(gameTime);

            // Update ripple effect
            if (_showRipple) {
                float rippleSpeed = 600f; // Adjust for faster/slower ripple
                _rippleRadius += rippleSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_rippleRadius >= _maxRippleRadius) {
                    _showRipple = false;
                    _rippleRadius = 0;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            MeasureTextIfNeeded();

            // Draw shadow if enabled
            if (DrawButtonShadow) {
                DrawButtonWithShadow(spriteBatch);
            } else {
                DrawButtonBackground(spriteBatch);
            }

            // Draw ripple effect
            if (_showRipple && EnableRippleEffect) {
                DrawRipple(spriteBatch);
            }

            // Calculate total content size for positioning
            Vector2 totalContentSize = CalculateTotalContentSize();

            // Get content position based on gravity
            Vector2 contentPosition = CalculateContentPosition(totalContentSize);

            // Apply pressed offset if button is in pressed state
            if (_state == StateOfButton.Pressed) {
                contentPosition.Y += PressedOffset;
            }

            // Draw icon if present
            if (Icon != null) {
                Vector2 iconPosition = contentPosition;

                // Draw the icon
                spriteBatch.Draw(
                    Icon,
                    iconPosition,
                    null,
                    IconColor * Opacity,
                    0f,
                    Vector2.Zero,
                    IconScale,
                    SpriteEffects.None,
                    0f
                );

                // Move content position for text if both icon and text exist
                if (!string.IsNullOrEmpty(Text)) {
                    contentPosition.X += Icon.Width * IconScale + ContentSpacing;
                }
            }

            // Draw text if present
            if (!string.IsNullOrEmpty(Text) && Font != null) {
                if (DrawTextShadow) {
                    // Draw text shadow
                    spriteBatch.DrawString(
                        Font,
                        Text,
                        contentPosition + TextShadowOffset,
                        TextShadowColor * Opacity,
                        0f,
                        Vector2.Zero,
                        TextSize,
                        SpriteEffects.None,
                        0f
                    );
                }

                if (DrawTextOutline) {
                    DrawTextWithOutline(spriteBatch, contentPosition);
                } else {
                    // Draw regular text
                    spriteBatch.DrawString(
                        Font,
                        Text,
                        contentPosition,
                        TextColor * Opacity,
                        0f,
                        Vector2.Zero,
                        TextSize,
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }

        private void DrawButtonBackground(SpriteBatch spriteBatch) {
            var texture = GetButtonTexture();

            if (texture.Width == 1 && texture.Height == 1) {
                // Single pixel texture case - fill the whole button
                spriteBatch.Draw(texture, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);
            } else {
                // Nine-slice the texture
                DrawNineSlice(spriteBatch, texture);
            }
        }

        private void DrawButtonWithShadow(SpriteBatch spriteBatch) {
            // Draw shadow first
            Texture2D shadowTexture = TextureHolder.Get($"{TextureDir}button_normal"); // Or use a dedicated shadow texture

            // Draw shadow offset from button
            var shadowRect = new Rectangle(
                (int)Position.X + ShadowSize / 2,
                (int)Position.Y + ShadowSize,
                (int)Size.X,
                (int)Size.Y
            );

            if (shadowTexture.Width == 1 && shadowTexture.Height == 1) {
                spriteBatch.Draw(shadowTexture, shadowRect, ShadowColor);
            } else {
                // Nine-slice the shadow
                Rectangle originalPos = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
                Vector2 originalPosition = Position;

                // Temporarily set position to shadow position
                Position = new Vector2(shadowRect.X, shadowRect.Y);
                DrawNineSlice(spriteBatch, shadowTexture, ShadowColor);

                // Restore original position
                Position = originalPosition;
            }

            // Then draw the button
            DrawButtonBackground(spriteBatch);
        }

        private void DrawRipple(SpriteBatch spriteBatch) {
            // Create a circle texture for ripple if needed
            Texture2D rippleTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            rippleTexture.SetData(new[] { Color.White });

            // Draw the circular ripple
            int rippleSize = (int)(_rippleRadius * 2);
            Rectangle rippleRect = new Rectangle(
                (int)(_rippleCenter.X - _rippleRadius),
                (int)(_rippleCenter.Y - _rippleRadius),
                rippleSize,
                rippleSize
            );

            spriteBatch.Draw(
                rippleTexture,
                rippleRect,
                null,
                _rippleColor,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                0f
            );
        }

        private void DrawTextWithOutline(SpriteBatch spriteBatch, Vector2 position) {
            // Draw outline with specified thickness
            for (int x = -TextOutlineWidth; x <= TextOutlineWidth; x++) {
                for (int y = -TextOutlineWidth; y <= TextOutlineWidth; y++) {
                    if (x != 0 || y != 0) {
                        Vector2 offset = new Vector2(x, y);
                        spriteBatch.DrawString(
                            Font,
                            Text,
                            position + offset,
                            TextOutlineColor * Opacity,
                            0f,
                            Vector2.Zero,
                            TextSize,
                            SpriteEffects.None,
                            0f
                        );
                    }
                }
            }

            // Draw text on top of outline
            spriteBatch.DrawString(
                Font,
                Text,
                position,
                TextColor * Opacity,
                0f,
                Vector2.Zero,
                TextSize,
                SpriteEffects.None,
                0f
            );
        }

        private Vector2 CalculateTotalContentSize() {
            Vector2 size = Vector2.Zero;

            // Calculate icon size if present
            if (Icon != null) {
                size.X += Icon.Width * IconScale;
                size.Y = Math.Max(size.Y, Icon.Height * IconScale);

                // Add spacing if we have both icon and text
                if (!string.IsNullOrEmpty(Text)) {
                    size.X += ContentSpacing;
                }
            }

            // Add text size if present
            if (!string.IsNullOrEmpty(Text) && Font != null) {
                size.X += _measuredTextSize.X;
                size.Y = Math.Max(size.Y, _measuredTextSize.Y);
            }

            return size;
        }

        private Vector2 CalculateContentPosition(Vector2 totalContentSize) {
            Vector2 position = Position;
            Vector2 availableSpace = Size;

            // Apply horizontal gravity
            if ((Gravity & Gravity.Left) != 0) {
                position.X += Padding;
            } else if ((Gravity & Gravity.Right) != 0) {
                position.X += availableSpace.X - totalContentSize.X - Padding;
            } else if ((Gravity & Gravity.CenterHorizontal) != 0) {
                position.X += (availableSpace.X - totalContentSize.X) / 2;
            } else {
                // Default to center if no horizontal gravity specified
                position.X += (availableSpace.X - totalContentSize.X) / 2;
            }

            // Apply vertical gravity
            if ((Gravity & Gravity.Top) != 0) {
                position.Y += Padding;
            } else if ((Gravity & Gravity.Bottom) != 0) {
                position.Y += availableSpace.Y - totalContentSize.Y - Padding;
            } else if ((Gravity & Gravity.CenterVertical) != 0) {
                position.Y += (availableSpace.Y - totalContentSize.Y) / 2;
            } else {
                // Default to center if no vertical gravity specified
                position.Y += (availableSpace.Y - totalContentSize.Y) / 2;
            }

            return position;
        }

        public override void HandleInput(UIEvent uiEvent) {
            if (!IsEnabled) {
                _state = StateOfButton.Disabled;
                return;
            }

            if (HitTest(Mouse.GetState().Position)) {
                if (uiEvent.Type == UIEventType.MouseDown) {
                    OnMouseDown?.Invoke();
                    _state = StateOfButton.Pressed;

                    // Start ripple effect if enabled
                    if (EnableRippleEffect) {
                        _showRipple = true;
                        _rippleCenter = new Vector2(
                            Mouse.GetState().X - Position.X,
                            Mouse.GetState().Y - Position.Y
                        );
                        _rippleRadius = 0;
                        // Calculate maximum ripple size based on button dimensions
                        _maxRippleRadius = Math.Max(Size.X, Size.Y);
                    }
                } else if (uiEvent.Type == UIEventType.MouseUp && _state == StateOfButton.Pressed) {
                    OnMouseUp?.Invoke();
                    OnClick?.Invoke();
                    _state = StateOfButton.Hovered;
                } else if (_state != StateOfButton.Pressed) {
                    OnMouseEnter?.Invoke();
                    _state = StateOfButton.Hovered;
                }
            } else {
                if (_state != StateOfButton.Normal && _state != StateOfButton.Disabled) {
                    OnMouseLeave?.Invoke();
                }
                _state = StateOfButton.Normal;
            }
        }

        private Texture2D GetButtonTexture() {
            Texture2D texture = new Texture2D(TextureHolder.Get($"{TextureDir}button_normal").GraphicsDevice, 1, 1);
            texture.SetData(new[] { BackgroundColor });

            try {
                switch (_state) {
                    case StateOfButton.Normal:
                        texture = TextureHolder.Get($"{TextureDir}button_normal");
                        break;
                    case StateOfButton.Hovered:
                        texture = TextureHolder.Get($"{TextureDir}button_hover");
                        break;
                    case StateOfButton.Pressed:
                        texture = TextureHolder.Get($"{TextureDir}button_pressed");
                        break;
                    case StateOfButton.Disabled:
                        texture = TextureHolder.Get($"{TextureDir}button_disabled");
                        break;
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error loading texture: {ex.Message}");
            }

            return texture;
        }

        private void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture, Color? color = null) {
            Color drawColor = color ?? Color.White;
            var scale = 7;
            var scaledCornerSize = CornerSize * scale;

            Rectangle srcTopLeft = new Rectangle(0, 0, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcTop = new Rectangle((int)CornerSize.X, 0, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);
            Rectangle srcTopRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, 0, (int)CornerSize.X, (int)CornerSize.Y);

            Rectangle srcBottomLeft = new Rectangle(0, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottomRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottom = new Rectangle((int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);

            Rectangle srcMiddle = new Rectangle((int)CornerSize.X, (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleLeft = new Rectangle(0, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));

            Rectangle dstTopLeft = new Rectangle((int)Position.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstTop = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);
            Rectangle dstTopRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);

            Rectangle dstBottomLeft = new Rectangle((int)Position.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottomRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottom = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);

            Rectangle dstMiddle = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleLeft = new Rectangle((int)Position.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));

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

        // Helper method to set up a common elevated button style
        public void SetElevatedButtonStyle() {
            DrawButtonShadow = true;
            ShadowSize = 4;
            Gravity = Gravity.Center;
            BackgroundColor = Color.White;
            EnableRippleEffect = true;
        }

        // Helper method to set up a flat button style
        public void SetFlatButtonStyle() {
            DrawButtonShadow = false;
            BackgroundColor = Color.Transparent;
            TextColor = new Color(33, 150, 243); // Material Design primary blue
            EnableRippleEffect = true;
        }

        // Helper method to set up an outlined button style
        public void SetOutlinedButtonStyle() {
            DrawButtonShadow = false;
            BackgroundColor = Color.Transparent;
            TextColor = new Color(33, 150, 243); // Material Design primary blue
            DrawTextOutline = true;
            TextOutlineColor = new Color(33, 150, 243, 128);
            TextOutlineWidth = 1;
            EnableRippleEffect = true;
        }
    }
}