using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Client.Component {
    public class UIManager {
        private List<List<IComponent>> _components = new();
        private IComponent _focusedComponent = null!;
        private readonly object _lock = new();

        public void Clear() {
            _focusedComponent = null!;
            foreach (var layer in _components) {
                layer.Clear();
            }
        }

        public void AddComponent(IComponent component, int layer = 0) {
            lock (_lock) {
                while (_components.Count <= layer) {
                    _components.Add(new List<IComponent>());
                }
                _components[layer].Add(component);
            }
        }

        public void RemoveComponent(IComponent component) {
            lock (_lock) {
                foreach (var layer in _components) {
                    if (layer.Remove(component)) {
                        break;
                    }
                }
            }
        }

        public void Update(GameTime gameTime) {
            lock (_lock) {
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
        }

        public void Draw(SpriteBatch spriteBatch) {
            lock (_lock) {
                for (int i = _components.Count - 1; i >= 0; i--) {
                    var layer = _components[i];
                    foreach (var component in layer) {
                        if (component.IsVisible) {
                            component.Draw(spriteBatch);
                        }
                    }
                }
            }
        }

        public void DispatchEvent(UIEvent uiEvent) {
            lock (_lock) {
                if (uiEvent.Type == UIEventType.KeyPress || uiEvent.Type == UIEventType.TextInput) {
                    _focusedComponent?.HandleInput(uiEvent);
                    return;
                }

                if (uiEvent.Type == UIEventType.MouseDown) {
                    for (int i = _components.Count - 1; i >= 0; i--) {
                        foreach (var component in _components[i]) {
                            if (component.IsVisible && component.IsEnabled && component.HitTest(uiEvent.MousePosition)) {
                                component.HandleInput(uiEvent);
                                SetFocus(component);
                                return;
                            }
                        }
                    }
                    ClearFocus();
                    return;
                }

                for (int i = _components.Count - 1; i >= 0; i--) {
                    var layer = _components[i];
                    foreach (var component in layer) {
                        if (component.IsVisible && component.IsEnabled) {
                            component.HandleInput(uiEvent);
                        }
                    }
                }
            }
        }

        private void ClearFocus() {
            SetFocus(null);
        }

        private void SetFocus(IComponent component) {
            lock (_lock) {
                if (_focusedComponent != null && _focusedComponent != component) {
                    _focusedComponent?.OnUnfocus();
                }

                _focusedComponent = component;
                _focusedComponent?.OnFocus();
            }
        }
    }
}
