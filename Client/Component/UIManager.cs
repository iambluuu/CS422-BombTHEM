using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Client.Component {
    public class UIManager {
        private List<List<IComponent>> _components = new();

        public void AddComponent(IComponent component, int layer = 0) {
            while (_components.Count <= layer) {
                _components.Add(new List<IComponent>());
            }
            _components[layer].Add(component);
        }

        public void RemoveComponent(IComponent component) {
            foreach (var layer in _components) {
                if (layer.Remove(component)) {
                    break;
                }
            }
        }

        public void Update(GameTime gameTime) {
            MouseState mouseState = Mouse.GetState();
            Point mousePos = new(mouseState.X, mouseState.Y);

            for (int i = _components.Count - 1; i >= 0; i--) {
                var layer = _components[i];
                foreach (var component in layer) {
                    if (component.IsVisible && component.IsEnabled) {
                        if (component.HitTest(mousePos)) {
                            component.OnMouseOver?.Invoke();
                        } else {
                            component.OnMouseOut?.Invoke();
                        }
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch) {
            for (int i = _components.Count - 1; i >= 0; i--) {
                var layer = _components[i];
                foreach (var component in layer) {
                    if (component.IsVisible) {
                        component.Draw(spriteBatch);
                    }
                }
            }
        }

        public void HandleMouseClick(Point mousePos) {
            for (int i = _components.Count - 1; i >= 0; i--) {
                var layer = _components[i];
                foreach (var component in layer) {
                    if (component.IsVisible && component.IsEnabled && component.HitTest(mousePos)) {
                        component.OnClick?.Invoke();
                        return;
                    }
                }
            }
        }
    }
}