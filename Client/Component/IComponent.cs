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

    public abstract class IComponent {
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public bool IsFocused { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public float Opacity { get; set; } = 1f;

        private Vector2 _position = Vector2.Zero;

        public virtual Vector2 Position {
            get => _position;
            set {
                _position = value;
                if (_position.X < 0) _position.X = 0;
                if (_position.Y < 0) _position.Y = 0;
            }
        }

        public virtual float X {
            get => Position.X;
            set => Position = new Vector2(value, Position.Y);
        }

        public virtual float Y {
            get => Position.Y;
            set => Position = new Vector2(Position.X, value);
        }

        private Vector2 _size = new Vector2(100, 100);

        public virtual Vector2 Size {
            get => _size;
            set {
                _size = value;
                if (_size.X < 0) _size.X = 0;
                if (_size.Y < 0) _size.Y = 0;
            }
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
            if (Size == Vector2.Zero) return; // Avoid division by zero
            if (parentRect.Width < Size.X || parentRect.Height < Size.Y) return; // Avoid negative position

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