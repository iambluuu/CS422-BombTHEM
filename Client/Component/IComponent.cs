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
        public float Opacity { get; set; } = 1f; // for fade in/out

        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 100);
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual bool HitTest(Point mousePos) {
            return mousePos.X >= Position.X && mousePos.X <= Position.X + Size.X &&
                   mousePos.Y >= Position.Y && mousePos.Y <= Position.Y + Size.Y;
        }

        public Action OnClick { get; set; } // Action to be invoked on click
        public Action OnMouseOver { get; set; } // Action to be invoked on mouse over
        public Action OnMouseOut { get; set; } // Action to be invoked on mouse out
        public Action OnMouseDown { get; set; } // Action to be invoked on mouse down
        public Action OnMouseUp { get; set; } // Action to be invoked on mouse up
    }
}