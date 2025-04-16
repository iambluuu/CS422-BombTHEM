using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Security.Principal;

namespace Client.Component {
    public class UIManager {
        private List<List<IComponent>> _components = new();
        private IComponent _focusedComponent = null!;

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
                        component.Update(gameTime);
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

        public void DispatchEvent(UIEvent e) {
            if (e.Type == UIEventType.MouseClick) {
                for (int i = _components.Count - 1; i >= 0; i--) {
                    var layer = _components[i];
                    foreach (var component in layer) {
                        if (component.IsVisible && component.IsEnabled && component.HitTest(e.MousePosition)) {
                            SetFocus(component);

                            component.HandleInput(e);
                            return;
                        }
                    }
                }

                ClearFocus();
            } else if (e.Type == UIEventType.TextInput || e.Type == UIEventType.KeyPress) {
                _focusedComponent?.HandleInput(e);
            }
        }

        private void ClearFocus() {
            SetFocus(null);
        }

        private void SetFocus(IComponent component) {
            if (_focusedComponent != null && _focusedComponent != component) {
                _focusedComponent?.OnUnfocus();
            }

            _focusedComponent = component;
            _focusedComponent?.OnFocus();
        }
    }
}