using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Component {
    public class LinearLayout : IComponent {
        public enum Orientation {
            Horizontal,
            Vertical
        }
        public Orientation LayoutOrientation { get; set; }
        public int Spacing { get; set; } = 5;
        public int Padding { get; set; } = 5;
        public List<IComponent> Components { get; set; } = new();

#nullable enable
        public LinearLayout(
            Orientation orientation = Orientation.Vertical,
            List<IComponent>? components = null,
            int spacing = 5,
            int padding = 5
        ) {
            LayoutOrientation = orientation;
            Spacing = spacing;
            Padding = padding;
            if (components != null) {
                Components = components;
            }
        }

        public void AddComponent(IComponent component) {
            Components.Add(component);
            switch (LayoutOrientation) {
                case Orientation.Horizontal:
                    float compWidth = (Size.X - Padding * 2 - Spacing * (Components.Count - 1)) / Components.Count;
                    for (int i = 0; i < Components.Count; i++) {
                        Components[i].Position = new Vector2(Padding + (compWidth + Spacing) * i, Position.Y + Padding);
                        Components[i].Size = new Vector2(compWidth, Size.Y - Padding * 2);
                    }
                    break;
                case Orientation.Vertical:
                    float compHeight = (Size.Y - Padding * 2 - Spacing * (Components.Count - 1)) / Components.Count;
                    for (int i = 0; i < Components.Count; i++) {
                        Components[i].Position = new Vector2(Position.X + Padding, Padding + (compHeight + Spacing) * i);
                        Components[i].Size = new Vector2(Size.X - Padding * 2, compHeight);
                    }
                    break;
            }
        }

        public override void Update(GameTime gameTime) {
            foreach (var component in Components) {
                component.Update(gameTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            int visibleCount = 0;
            for (int i = Components.Count - 1; i >= 0; i--) {
                if (Components[i].IsVisible) {
                    visibleCount++;
                }
            }
            if (visibleCount == 0) return;

            float compWidth =
                (LayoutOrientation == Orientation.Horizontal)
                ? (int)((Size.X - Padding * 2 - Spacing * (visibleCount - 1)) / visibleCount)
                : Size.X - Padding * 2;
            float compHeight =
                (LayoutOrientation == Orientation.Vertical)
                ? (int)((Size.Y - Padding * 2 - Spacing * (visibleCount - 1)) / visibleCount)
                : Size.Y - Padding * 2;

            Vector2 currentPosition = Position + new Vector2(Padding, Padding);

            foreach (var component in Components) {
                if (!component.IsVisible) continue;

                component.Position = currentPosition;
                component.Size = new Vector2(compWidth, compHeight);
                if (LayoutOrientation == Orientation.Horizontal) {
                    currentPosition.X += compWidth + Spacing;
                } else {
                    currentPosition.Y += compHeight + Spacing;
                }
                component.Draw(spriteBatch);

            }
        }
    }
}