using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Client.Component {
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
        public float Opacity { get; set; } = 1f; // for fade in/out

        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 100);
        public void Center(Rectangle parentRect) {
            if (parentRect == Rectangle.Empty) return;
            if (parentRect.Width == 0 || parentRect.Height == 0) return;
            if (Size == Vector2.Zero) return; // Avoid division by zero
            if (parentRect.Width < Size.X || parentRect.Height < Size.Y) return; // Avoid negative position

            Position = new Vector2(parentRect.X + (parentRect.Width - Size.X) / 2, parentRect.Y + (parentRect.Height - Size.Y) / 2);
        }

        public virtual void Update(GameTime gameTime) { }
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
    }
}