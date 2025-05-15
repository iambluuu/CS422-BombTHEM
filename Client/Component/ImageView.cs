using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Client.Component {
    public enum ScaleType {
        Center,
        CenterCrop,
        CenterInside,
        FitCenter,
        FitStart,
        FitEnd,
        FitXY,
        Matrix
    }

    public class ImageView : IComponent {
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
        public ScaleType ScaleType { get; set; } = ScaleType.FitCenter;
        public Gravity Gravity { get; set; } = Gravity.Center;
        public Color Tint { get; set; } = Color.White;
        public float Rotation { get; set; } = 0f;
        public Vector2 RotationOrigin { get; set; } = new Vector2(0.5f, 0.5f);
        public SpriteEffects SpriteEffects { get; set; } = SpriteEffects.None;
        public bool AdjustViewBounds { get; set; } = false;
        public float MaxWidth { get; set; } = float.MaxValue;
        public float MaxHeight { get; set; } = float.MaxValue;
        public bool DrawShadow { get; set; } = false;
        public Vector2 ShadowOffset { get; set; } = new Vector2(2, 2);
        public Color ShadowColor { get; set; } = new Color(0, 0, 0, 128);
        public float ShadowSoftness { get; set; } = 1f;
        private float _alpha = 1.0f;
        public float Alpha {
            get => _alpha;
            set {
                _alpha = MathHelper.Clamp(value, 0f, 1f);
            }
        }
        private bool _needsLayout = true;
        private Rectangle _sourceRectangle;
        private Rectangle _destinationRectangle;
        private Vector2 _rotationOriginVector = Vector2.Zero;

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

        // Fix for positioning - Override Position property
        public override Vector2 Position {
            get => base.Position;
            set {
                if (value != base.Position) {
                    base.Position = value;
                    _needsLayout = true;
                }
            }
        }

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
            if (_needsLayout) {
                PerformLayout();
            }
        }

        private void PerformLayout() {
            if (Texture == null) {
                _sourceRectangle = Rectangle.Empty;
                _destinationRectangle = Rectangle.Empty;
                _needsLayout = false;
                return;
            }

            // Get available content area dimensions
            float contentWidth = Width - PaddingLeft - PaddingRight;
            float contentHeight = Height - PaddingTop - PaddingBottom;

            // Set source rectangle to the full texture
            _sourceRectangle = new Rectangle(0, 0, Texture.Width, Texture.Height);

            float destWidth = 0;
            float destHeight = 0;

            // Calculate aspect ratios
            float srcAspectRatio = (float)Texture.Width / Texture.Height;
            float destAspectRatio = contentWidth / contentHeight;

            // Determine destination dimensions based on ScaleType
            switch (ScaleType) {
                case ScaleType.Center:
                    destWidth = Texture.Width;
                    destHeight = Texture.Height;
                    break;

                case ScaleType.CenterCrop:
                    if (srcAspectRatio > destAspectRatio) {
                        destHeight = contentHeight;
                        destWidth = destHeight * srcAspectRatio;
                    } else {
                        destWidth = contentWidth;
                        destHeight = destWidth / srcAspectRatio;
                    }
                    break;

                case ScaleType.CenterInside:
                    if (Texture.Width <= contentWidth && Texture.Height <= contentHeight) {
                        destWidth = Texture.Width;
                        destHeight = Texture.Height;
                    } else {
                        if (srcAspectRatio > destAspectRatio) {
                            destWidth = contentWidth;
                            destHeight = destWidth / srcAspectRatio;
                        } else {
                            destHeight = contentHeight;
                            destWidth = destHeight * srcAspectRatio;
                        }
                    }
                    break;

                case ScaleType.FitCenter:
                case ScaleType.FitStart:
                case ScaleType.FitEnd:
                    if (srcAspectRatio > destAspectRatio) {
                        destWidth = contentWidth;
                        destHeight = destWidth / srcAspectRatio;
                    } else {
                        destHeight = contentHeight;
                        destWidth = destHeight * srcAspectRatio;
                    }
                    break;

                case ScaleType.FitXY:
                    destWidth = contentWidth;
                    destHeight = contentHeight;
                    break;

                case ScaleType.Matrix:
                    destWidth = contentWidth;
                    destHeight = contentHeight;
                    break;
            }

            // Calculate the destination X position
            float destX = Position.X + PaddingLeft;
            if ((Gravity & Gravity.CenterHorizontal) != 0 || ScaleType == ScaleType.Center ||
                ScaleType == ScaleType.CenterCrop || ScaleType == ScaleType.CenterInside ||
                ScaleType == ScaleType.FitCenter) {
                destX = Position.X + PaddingLeft + (contentWidth - destWidth) / 2;
            } else if ((Gravity & Gravity.Right) != 0 || ScaleType == ScaleType.FitEnd) {
                destX = Position.X + Width - PaddingRight - destWidth;
            } else if ((Gravity & Gravity.Left) != 0 || ScaleType == ScaleType.FitStart) {
                destX = Position.X + PaddingLeft;
            }

            // Calculate the destination Y position
            float destY = Position.Y + PaddingTop;
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

            // Calculate rotation origin vector for proper rotation
            _rotationOriginVector = new Vector2(
                _sourceRectangle.Width * RotationOrigin.X,
                _sourceRectangle.Height * RotationOrigin.Y
            );

            _needsLayout = false;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (Texture == null || !IsVisible || Alpha <= 0) return;

            if (_needsLayout) {
                PerformLayout();
            }

            // Calculate rotation origin point in destination coordinates
            Vector2 origin = Vector2.Zero; // Default for drawing without rotation

            if (Rotation != 0) {
                // Only calculate an origin if rotation is used
                origin = new Vector2(
                    _destinationRectangle.Width * RotationOrigin.X,
                    _destinationRectangle.Height * RotationOrigin.Y
                );
            }

            // Draw shadow if enabled
            if (DrawShadow) {
                for (int i = 0; i < ShadowSoftness; i++) {
                    float softnessScale = (i + 1) / ShadowSoftness;
                    Vector2 shadowOffset = ShadowOffset * softnessScale;

                    if (Rotation == 0) {
                        // For non-rotated images, just offset the rectangle
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
                            0,
                            Vector2.Zero,
                            SpriteEffects,
                            0.01f
                        );
                    } else {
                        // For rotated images, use the overload that takes a Vector2 for position
                        Vector2 shadowPos = new Vector2(
                            _destinationRectangle.X + shadowOffset.X,
                            _destinationRectangle.Y + shadowOffset.Y
                        );

                        spriteBatch.Draw(
                            Texture,
                            shadowPos,
                            _sourceRectangle,
                            ShadowColor * Opacity * Alpha * (1 - 0.3f * softnessScale),
                            Rotation,
                            _rotationOriginVector,
                            new Vector2((float)_destinationRectangle.Width / Texture.Width,
                                       (float)_destinationRectangle.Height / Texture.Height),
                            SpriteEffects,
                            0.01f
                        );
                    }
                }
            }

            // Draw the main image
            if (Rotation == 0) {
                // Use rectangle overload for non-rotated images for efficiency
                spriteBatch.Draw(
                    Texture,
                    _destinationRectangle,
                    _sourceRectangle,
                    Tint * Opacity * Alpha,
                    0,
                    Vector2.Zero,
                    SpriteEffects,
                    0f
                );
            } else {
                // Use Vector2 position overload for rotated images
                Vector2 position = new Vector2(_destinationRectangle.X, _destinationRectangle.Y);
                Vector2 scale = new Vector2(
                    (float)_destinationRectangle.Width / Texture.Width,
                    (float)_destinationRectangle.Height / Texture.Height
                );

                spriteBatch.Draw(
                    Texture,
                    position,
                    _sourceRectangle,
                    Tint * Opacity * Alpha,
                    Rotation,
                    Vector2.Zero,
                    scale,
                    SpriteEffects,
                    0f
                );
            }
        }

        public void SetAdjustViewBounds(bool adjustViewBounds) {
            if (AdjustViewBounds != adjustViewBounds) {
                AdjustViewBounds = adjustViewBounds;
                _needsLayout = true;
            }
        }

        public void FadeIn(float duration = 0.5f) {
            Alpha = 1.0f;
        }

        public void FadeOut(float duration = 0.5f) {
            Alpha = 0.0f;
        }

        public void RequestLayout() {
            _needsLayout = true;
        }

        public void SetPadding(float left, float top, float right, float bottom) {
            PaddingLeft = left;
            PaddingTop = top;
            PaddingRight = right;
            PaddingBottom = bottom;
            _needsLayout = true;
        }
    }
}
