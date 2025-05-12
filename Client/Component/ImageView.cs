using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Client.Component {
    // Android-like scale types
    public enum ScaleType {
        Center,           // Center the image without scaling
        CenterCrop,       // Scale the image uniformly to fill the bounds and crop if necessary
        CenterInside,     // Scale the image uniformly to fit within the bounds
        FitCenter,        // Scale the image uniformly to fit within the bounds and center it
        FitStart,         // Scale the image uniformly to fit within the bounds, align to the start
        FitEnd,           // Scale the image uniformly to fit within the bounds, align to the end
        FitXY,            // Scale the image to fill the bounds (may distort the image)
        Matrix            // Use a custom transformation matrix (not fully implemented)
    }

    public class ImageView : IComponent {
        // Image properties
        private Texture2D _texture;
        public required Texture2D Texture {
            get => _texture;
            set {
                if (_texture != value) {
                    _texture = value;
                    _needsLayout = true;
                }
            }
        }

        // Android-like properties
        public ScaleType ScaleType { get; set; } = ScaleType.FitCenter;
        public Gravity Gravity { get; set; } = Gravity.Center;
        public Color Tint { get; set; } = Color.White;

        // Image adjustment properties
        public float Rotation { get; set; } = 0f;
        public Vector2 RotationOrigin { get; set; } = new Vector2(0.5f, 0.5f); // Normalized (0-1) center point
        public SpriteEffects SpriteEffects { get; set; } = SpriteEffects.None;
        public bool AdjustViewBounds { get; set; } = false;
        public float MaxWidth { get; set; } = float.MaxValue;
        public float MaxHeight { get; set; } = float.MaxValue;

        // Visual effects
        public bool DrawShadow { get; set; } = false;
        public Vector2 ShadowOffset { get; set; } = new Vector2(2, 2);
        public Color ShadowColor { get; set; } = new Color(0, 0, 0, 128);
        public float ShadowSoftness { get; set; } = 1f; // Simulated with multiple shadow draws

        // Animation properties
        private float _alpha = 1.0f;
        public float Alpha {
            get => _alpha;
            set {
                _alpha = MathHelper.Clamp(value, 0f, 1f);
            }
        }

        // Layout flag
        private bool _needsLayout = true;
        private Rectangle _sourceRectangle;
        private Rectangle _destinationRectangle;

        public override Vector2 Size {
            get => base.Size;
            set {
                if (value != base.Size) {
                    base.Size = value;
                    _needsLayout = true;
                }
            }
        }

        public override SizeMode WidthMode {
            get => base.WidthMode;
            set {
                if (value != base.WidthMode) {
                    base.WidthMode = value;
                    _needsLayout = true;
                }
            }
        }

        public override SizeMode HeightMode {
            get => base.HeightMode;
            set {
                if (value != base.HeightMode) {
                    base.HeightMode = value;
                    _needsLayout = true;
                }
            }
        }

        public override float Width {
            get => base.Width;
            set {
                if (value != base.Width) {
                    base.Width = value;
                    _needsLayout = true;
                }
            }
        }

        public override float Height {
            get => base.Height;
            set {
                if (value != base.Height) {
                    base.Height = value;
                    _needsLayout = true;
                }
            }
        }

        // Android-like content sizing
        protected override float MeasureContentWidth() {
            if (Texture == null)
                return PaddingLeft + PaddingRight;

            if (AdjustViewBounds) {
                float aspectRatio = (float)Texture.Width / Texture.Height;
                float height = Math.Min(MaxHeight, Size.Y - PaddingTop - PaddingBottom);
                return Math.Min(MaxWidth, height * aspectRatio) + PaddingLeft + PaddingRight;
            }

            return Math.Min(MaxWidth, Texture.Width) + PaddingLeft + PaddingRight;
        }

        protected override float MeasureContentHeight() {
            if (Texture == null)
                return PaddingTop + PaddingBottom;

            if (AdjustViewBounds) {
                float aspectRatio = (float)Texture.Width / Texture.Height;
                float width = Math.Min(MaxWidth, Size.X - PaddingLeft - PaddingRight);
                return Math.Min(MaxHeight, width / aspectRatio) + PaddingTop + PaddingBottom;
            }

            return Math.Min(MaxHeight, Texture.Height) + PaddingTop + PaddingBottom;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            // Perform layout if needed
            // if (_needsLayout) {
            PerformLayout();
            // }
        }

        private void PerformLayout() {
            if (Texture == null) {
                _sourceRectangle = Rectangle.Empty;
                _destinationRectangle = Rectangle.Empty;
                _needsLayout = false;
                return;
            }

            // Calculate the content area (excluding padding)
            float contentWidth = Width - PaddingLeft - PaddingRight;
            float contentHeight = Height - PaddingTop - PaddingBottom;

            // Source rectangle (the entire texture)
            _sourceRectangle = new Rectangle(0, 0, Texture.Width, Texture.Height);

            // Calculate dimensions based on ScaleType
            float destWidth = 0;
            float destHeight = 0;
            float destX = Position.X + PaddingLeft;
            float destY = Position.Y + PaddingTop;

            float srcAspectRatio = (float)Texture.Width / Texture.Height;
            float destAspectRatio = contentWidth / contentHeight;

            switch (ScaleType) {
                case ScaleType.Center:
                    // Center the image without scaling
                    destWidth = Texture.Width;
                    destHeight = Texture.Height;
                    break;

                case ScaleType.CenterCrop:
                    // Scale to fill, maintaining aspect ratio, and crop if necessary
                    if (srcAspectRatio > destAspectRatio) {
                        // Image is wider than destination: match height and crop width
                        destHeight = contentHeight;
                        destWidth = destHeight * srcAspectRatio;
                    } else {
                        // Image is taller than destination: match width and crop height
                        destWidth = contentWidth;
                        destHeight = destWidth / srcAspectRatio;
                    }
                    break;

                case ScaleType.CenterInside:
                    // Scale to fit inside, maintaining aspect ratio
                    if (Texture.Width <= contentWidth && Texture.Height <= contentHeight) {
                        // Image is smaller than bounds: don't scale
                        destWidth = Texture.Width;
                        destHeight = Texture.Height;
                    } else {
                        // Scale down to fit
                        if (srcAspectRatio > destAspectRatio) {
                            // Width is the constraint
                            destWidth = contentWidth;
                            destHeight = destWidth / srcAspectRatio;
                        } else {
                            // Height is the constraint
                            destHeight = contentHeight;
                            destWidth = destHeight * srcAspectRatio;
                        }
                    }
                    break;

                case ScaleType.FitCenter:
                case ScaleType.FitStart:
                case ScaleType.FitEnd:
                    // Scale to fit inside, maintaining aspect ratio
                    if (srcAspectRatio > destAspectRatio) {
                        // Width is the constraint
                        destWidth = contentWidth;
                        destHeight = destWidth / srcAspectRatio;
                    } else {
                        // Height is the constraint
                        destHeight = contentHeight;
                        destWidth = destHeight * srcAspectRatio;
                    }
                    break;

                case ScaleType.FitXY:
                    // Scale to fill the bounds (may distort)
                    destWidth = contentWidth;
                    destHeight = contentHeight;
                    break;

                case ScaleType.Matrix:
                    // Custom matrix transformation - use defaults for now
                    destWidth = contentWidth;
                    destHeight = contentHeight;
                    break;
            }

            // Apply Gravity for positioning
            if ((Gravity & Gravity.CenterHorizontal) != 0 || ScaleType == ScaleType.Center ||
                ScaleType == ScaleType.CenterCrop || ScaleType == ScaleType.CenterInside ||
                ScaleType == ScaleType.FitCenter) {
                destX = Position.X + PaddingLeft + (contentWidth - destWidth) / 2;
            } else if ((Gravity & Gravity.Right) != 0 || ScaleType == ScaleType.FitEnd) {
                destX = Position.X + Width - PaddingRight - destWidth;
            }

            if ((Gravity & Gravity.CenterVertical) != 0 || ScaleType == ScaleType.Center ||
                ScaleType == ScaleType.CenterCrop || ScaleType == ScaleType.CenterInside ||
                ScaleType == ScaleType.FitCenter) {
                destY = Position.Y + PaddingTop + (contentHeight - destHeight) / 2;
            } else if ((Gravity & Gravity.Bottom) != 0 || ScaleType == ScaleType.FitEnd) {
                destY = Position.Y + Height - PaddingBottom - destHeight;
            }

            _destinationRectangle = new Rectangle(
                (int)destX,
                (int)destY,
                (int)destWidth,
                (int)destHeight
            );

            _needsLayout = false;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (Texture == null || !IsVisible || Alpha <= 0) return;

            // Ensure layout is up to date
            if (_needsLayout) {
                PerformLayout();
            }

            // Calculate actual rotation origin in pixels
            Vector2 origin = new Vector2(
                _sourceRectangle.Width * RotationOrigin.X,
                _sourceRectangle.Height * RotationOrigin.Y
            );

            // Draw shadow if enabled
            if (DrawShadow) {
                // Simple shadow implementation with multiple draws for softness
                for (int i = 0; i < ShadowSoftness; i++) {
                    float softnessScale = (i + 1) / ShadowSoftness;
                    Vector2 shadowOffset = ShadowOffset * softnessScale;
                    Rectangle shadowRect = new Rectangle(
                        _destinationRectangle.X + (int)shadowOffset.X,
                        _destinationRectangle.Y + (int)shadowOffset.Y,
                        _destinationRectangle.Width,
                        _destinationRectangle.Height
                    );

                    spriteBatch.Draw(
                        Texture,
                        shadowRect,
                        _sourceRectangle,
                        ShadowColor * Opacity * Alpha * (1 - 0.3f * softnessScale),
                        Rotation,
                        Vector2.Zero, // temporary fix
                        SpriteEffects,
                        0.01f
                    );
                }
            }

            // Draw the image
            spriteBatch.Draw(
                Texture,
                _destinationRectangle,
                _sourceRectangle,
                Tint * Opacity * Alpha,
                Rotation,
                Vector2.Zero, // temporary fix
                SpriteEffects,
                0f
            );
        }

        // Android-like methods for adjusting view bounds based on image aspect ratio
        public void SetAdjustViewBounds(bool adjustViewBounds) {
            if (AdjustViewBounds != adjustViewBounds) {
                AdjustViewBounds = adjustViewBounds;
                _needsLayout = true;
            }
        }

        // Android-like animations
        public void FadeIn(float duration = 0.5f) {
            // Implement animation logic here or in an animation system
            Alpha = 1.0f;
        }

        public void FadeOut(float duration = 0.5f) {
            // Implement animation logic here or in an animation system
            Alpha = 0.0f;
        }

        // Request a layout update (Android-like)
        public void RequestLayout() {
            _needsLayout = true;
        }

        // Convenience method for setting padding
        public void SetPadding(float left, float top, float right, float bottom) {
            PaddingLeft = left;
            PaddingTop = top;
            PaddingRight = right;
            PaddingBottom = bottom;
            _needsLayout = true;
        }
    }
}
