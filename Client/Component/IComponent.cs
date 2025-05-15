using Client.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Client.Component {
    public struct Padding {
        public float Left, Top, Right, Bottom;

        public Padding(float all) => Left = Top = Right = Bottom = all;

        public Padding(float vertical, float horizontal) {
            Left = Right = horizontal;
            Top = Bottom = vertical;
        }

        public Padding(float left, float top, float right, float bottom) {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public float Horizontal => Left + Right;
        public float Vertical => Top + Bottom;
    }

    public enum ContentAlignment {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    // Android-like gravity (combined vertical and horizontal alignment)
    [Flags]
    public enum Gravity {
        NoGravity = 0,
        Top = 0x01,
        Bottom = 0x02,
        Left = 0x04,
        Right = 0x08,
        CenterVertical = 0x10,
        CenterHorizontal = 0x20,

        // Convenience combinations
        Center = CenterVertical | CenterHorizontal,
        TopLeft = Top | Left,
        TopRight = Top | Right,
        BottomLeft = Bottom | Left,
        BottomRight = Bottom | Right,
        CenterLeft = CenterVertical | Left,
        CenterRight = CenterVertical | Right,
        BottomCenter = CenterHorizontal | Bottom,
        TopCenter = CenterHorizontal | Top,
    }

    // Android-like layout parameters
    public enum SizeMode {
        Fixed,       // Use exact pixel dimensions (like specific dp value)
        MatchParent, // Take full size of parent (like match_parent)
        WrapContent  // Size based on content (like wrap_content)
    }

    public abstract class IComponent {
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public bool IsFocused { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public float Opacity { get; set; } = 1f;

        // Layout weight (Android-like)
        public int Weight { get; set; } = 0;

        // Padding (moved from LinearLayout)
        private Padding _padding = new(0);
        public virtual float Padding { get => _padding.Left; set { _padding = new Padding(value); } }
        public virtual float PaddingLeft { get => _padding.Left; set { _padding.Left = value; } }
        public virtual float PaddingRight { get => _padding.Right; set { _padding.Right = value; } }
        public virtual float PaddingTop { get => _padding.Top; set { _padding.Top = value; } }
        public virtual float PaddingBottom { get => _padding.Bottom; set { _padding.Bottom = value; } }
        public Padding PaddingStruct { get => _padding; set { _padding = value; } }

        public Gravity LayoutGravity { get; set; } = Gravity.NoGravity;

        // Size modes (Android-like)
        public virtual SizeMode WidthMode { get; set; } = SizeMode.Fixed;
        public virtual SizeMode HeightMode { get; set; } = SizeMode.Fixed;
        public virtual Vector2 Position { get; set; } = new Vector2(0, 0);

        public virtual float X {
            get => Position.X;
            set => Position = new Vector2(value, Position.Y);
        }

        public virtual float Y {
            get => Position.Y;
            set => Position = new Vector2(Position.X, value);
        }

        private Vector2 _size = new(100, 100);
        private Vector2 _measuredSize = new(100, 100); // Size calculated by parent for match_parent or wrap_content

        public virtual Vector2 Size {
            get {
                if (WidthMode == SizeMode.Fixed && HeightMode == SizeMode.Fixed) {
                    return _size;
                } else {
                    Vector2 resultSize = new Vector2(
                        WidthMode == SizeMode.Fixed ? _size.X : _measuredSize.X,
                        HeightMode == SizeMode.Fixed ? _size.Y : _measuredSize.Y
                    );
                    return resultSize;
                }
            }
            set {
                _size = value;
                if (_size.X < 0) _size.X = 0;
                if (_size.Y < 0) _size.Y = 0;
            }
        }

        // Method for parent layouts to set measured size for match_parent and wrap_content
        public void SetMeasuredSize(float width, float height) {
            _measuredSize.X = width;
            _measuredSize.Y = height;
        }

        // Return the current desired width taking into account SizeMode
        public float RequestedWidth {
            get {
                switch (WidthMode) {
                    case SizeMode.Fixed:
                        return _size.X;
                    case SizeMode.MatchParent:
                        return 0; // Will be determined by parent
                    case SizeMode.WrapContent:
                        return MeasureContentWidth();
                    default:
                        return _size.X;
                }
            }
        }

        // Return the current desired height taking into account SizeMode
        public float RequestedHeight {
            get {
                switch (HeightMode) {
                    case SizeMode.Fixed:
                        return _size.Y;
                    case SizeMode.MatchParent:
                        return 0; // Will be determined by parent
                    case SizeMode.WrapContent:
                        return MeasureContentHeight();
                    default:
                        return _size.Y;
                }
            }
        }

        // Override in subclasses to provide content measurement for wrap_content
        protected virtual float MeasureContentWidth() {
            return _size.X; // Default implementation
        }

        protected virtual float MeasureContentHeight() {
            return _size.Y; // Default implementation
        }

        public virtual float Width {
            get => Size.X;
            set => Size = new Vector2(value, Size.Y);
        }

        public virtual float Height {
            get => Size.Y;
            set => Size = new Vector2(Size.X, value);
        }

        public void Center(Rectangle parentRect) {
            if (parentRect == Rectangle.Empty) return;
            if (parentRect.Width == 0 || parentRect.Height == 0) return;
            if (Size == Vector2.Zero) return;
            if (parentRect.Width < Size.X || parentRect.Height < Size.Y) return;

            Position = new Vector2(parentRect.X + (parentRect.Width - Size.X) / 2, parentRect.Y + (parentRect.Height - Size.Y) / 2);
        }

        public virtual void Update(GameTime gameTime) {
            for (int i = _animations.Count - 1; i >= 0; i--) {
                var anim = _animations[i];
                anim.Update(this, gameTime);
                if (anim.IsFinished)
                    _animations.RemoveAt(i);
            }
        }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual bool HitTest(Point mousePos) {
            return mousePos.X >= Position.X && mousePos.X <= Position.X + Size.X &&
                   mousePos.Y >= Position.Y && mousePos.Y <= Position.Y + Size.Y;
        }

        public virtual void HandleInput(UIEvent uiEvent) { }

        public Action OnClick { get; set; } // Action to be invoked on click
        public Action OnMouseDown { get; set; } // Action to be invoked on mouse down
        public Action OnMouseUp { get; set; } // Action to be invoked on mouse up
        public Action OnMouseEnter { get; set; } // Action to be invoked on mouse enter
        public Action OnMouseLeave { get; set; } // Action to be invoked on mouse leave

        public virtual void OnFocus() { IsFocused = true; }
        public virtual void OnUnfocus() { IsFocused = false; }

        private List<IUIAnimation> _animations = new List<IUIAnimation>();
        public void AddAnimation(IUIAnimation animation) {
            _animations.Add(animation);
            animation.OnStart(this);
        }
    }
}
