using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Component {
    public class LinearLayout : IComponent {
        private readonly Vector2 CornerSize = new(5, 5);
        private readonly Rectangle TextureSize = new(0, 0, 16, 16);
        private const string TextureDir = "Texture/Theme/";

        public enum Orientation {
            Horizontal,
            Vertical
        }

        public Orientation LayoutOrientation { get; set; }
        public int Spacing { get; set; } = 5;
        public int Padding { get; set; } = 5;
        public List<IComponent> Components { get; set; } = new();
        public List<int> Weights { get; set; } = new();
        private int _totalWeight = 0;

        public readonly bool HasBackground = true;
#nullable enable
        private IComponent _focusedComponent = null!;
        public LinearLayout(
            Orientation orientation = Orientation.Vertical,
            List<IComponent>? components = null,
            bool hasBackground = true,
            int spacing = 5,
            int padding = 5
        ) {
            LayoutOrientation = orientation;
            Spacing = spacing;
            Padding = padding;
            HasBackground = hasBackground;
            if (components != null) {
                Components = components;
            } else {
                Components = new();
            }
        }

        public override Vector2 Position {
            get => base.Position;
            set {
                base.Position = value;
                RearrangeComponents();
            }
        }
        public override Vector2 Size {
            get => base.Size;
            set {
                base.Size = value;
                RearrangeComponents();
            }
        }

        private void RearrangeComponents() {
            var currentPosition = Position + new Vector2(Padding, Padding);
            for (int i = 0; i < Components.Count; i++) {
                if (!Components[i].IsVisible) continue;

                if (LayoutOrientation == Orientation.Horizontal) {
                    var compWidth = (Size.X - Padding * 2 - Spacing * (Components.Count - 1)) * Weights[i] / _totalWeight;
                    Components[i].Position = currentPosition;
                    Components[i].Size = new Vector2(compWidth, Size.Y - Padding * 2);
                    currentPosition.X += compWidth + Spacing;
                } else {
                    var compHeight = (Size.Y - Padding * 2 - Spacing * (Components.Count - 1)) * Weights[i] / _totalWeight;
                    Components[i].Position = currentPosition;
                    Components[i].Size = new Vector2(Size.X - Padding * 2, compHeight);
                    currentPosition.Y += compHeight + Spacing;
                }
            }
        }

        public void AddComponent(IComponent component, int weight = 1, int position = -1) {
            if (position < 0 || position >= Components.Count) {
                Components.Add(component);
                Weights.Add(weight);
            } else {
                Components.Insert(position, component);
                Weights.Insert(position, weight);
            }
            _totalWeight += weight;
            RearrangeComponents();
        }

        public override void Update(GameTime gameTime) {
            foreach (var component in Components) {
                component.Update(gameTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            // Draw red outline for debugging
            Texture2D texture = GetTexture();
            if (texture != null) {
                DrawNineSlice(spriteBatch, texture);
            } else {
                texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                texture.SetData(new[] { Color.Red * 0.5f });
                spriteBatch.Draw(texture, Position, Color.Red * 0.5f);
            }

            int visibleCount = 0;
            for (int i = Components.Count - 1; i >= 0; i--) {
                if (Components[i].IsVisible) {
                    visibleCount++;
                }
            }
            if (visibleCount == 0) return;

            for (int i = Components.Count - 1; i >= 0; i--) {
                if (Components[i].IsVisible) {
                    Components[i].Draw(spriteBatch);
                }
            }
        }

        public override void HandleInput(UIEvent uiEvent) {
            if (!IsEnabled) return;
            if (uiEvent.Type == UIEventType.KeyPress || uiEvent.Type == UIEventType.TextInput) {
                _focusedComponent?.HandleInput(uiEvent);
                return;
            }

            if (uiEvent.Type == UIEventType.MouseDown) {
                for (int i = Components.Count - 1; i >= 0; i--) {
                    var component = Components[i];
                    if (component.IsVisible && component.IsEnabled && component.HitTest(uiEvent.MousePosition)) {
                        component.HandleInput(uiEvent);
                        SetFocus(component);
                        return;
                    }
                }
                ClearFocus();
                return;
            }

            for (int i = Components.Count - 1; i >= 0; i--) {
                var component = Components[i];
                if (component.IsVisible && component.IsEnabled) {
                    component.HandleInput(uiEvent);
                }
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

        public override void OnFocus() {
            IsFocused = true;
        }

        public override void OnUnfocus() {
            IsFocused = false;
            foreach (var component in Components) {
                component.OnUnfocus();
            }
        }

        private Texture2D GetTexture() {
            if (!HasBackground) return null!;

            Texture2D texture = new Texture2D(TextureHolder.Get($"{TextureDir}nine_path_panel_2").GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.Red * 0.5f });

            try {
                texture = TextureHolder.Get($"{TextureDir}nine_path_panel_2", TextureSize);
            } catch (System.IO.FileNotFoundException ex) {
                Console.WriteLine($"Texture not found: {ex.Message}");
                return null;
            } catch (Exception ex) {
                Console.WriteLine($"Error loading texture: {ex.Message}");
                return null;
            }

            return texture;
        }

        private void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture) {
            var scale = 5;
            var scaledCornerSize = CornerSize * scale;

            Rectangle srcTopLeft = new Rectangle(0, 0, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcTop = new Rectangle((int)CornerSize.X, 0, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);
            Rectangle srcTopRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, 0, (int)CornerSize.X, (int)CornerSize.Y);

            Rectangle srcBottomLeft = new Rectangle(0, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottomRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)CornerSize.X, (int)CornerSize.Y);
            Rectangle srcBottom = new Rectangle((int)CornerSize.X, TextureSize.Height - (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)CornerSize.Y);

            Rectangle srcMiddle = new Rectangle((int)CornerSize.X, (int)CornerSize.Y, (int)(TextureSize.Width - CornerSize.X * 2), (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleLeft = new Rectangle(0, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));
            Rectangle srcMiddleRight = new Rectangle(TextureSize.Width - (int)CornerSize.X, (int)CornerSize.Y, (int)CornerSize.X, (int)(TextureSize.Height - CornerSize.Y * 2));

            Rectangle dstTopLeft = new Rectangle((int)Position.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstTop = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);
            Rectangle dstTopRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);

            Rectangle dstBottomLeft = new Rectangle((int)Position.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottomRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)scaledCornerSize.Y);
            Rectangle dstBottom = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)Size.Y - (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)scaledCornerSize.Y);

            Rectangle dstMiddle = new Rectangle((int)Position.X + (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)(Size.X - scaledCornerSize.X * 2), (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleLeft = new Rectangle((int)Position.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));
            Rectangle dstMiddleRight = new Rectangle((int)Position.X + (int)Size.X - (int)scaledCornerSize.X, (int)Position.Y + (int)scaledCornerSize.Y, (int)scaledCornerSize.X, (int)(Size.Y - scaledCornerSize.Y * 2));

            spriteBatch.Draw(texture, dstTopLeft, srcTopLeft, Color.White);
            spriteBatch.Draw(texture, dstTop, srcTop, Color.White);
            spriteBatch.Draw(texture, dstTopRight, srcTopRight, Color.White);

            spriteBatch.Draw(texture, dstBottomLeft, srcBottomLeft, Color.White);
            spriteBatch.Draw(texture, dstBottom, srcBottom, Color.White);
            spriteBatch.Draw(texture, dstBottomRight, srcBottomRight, Color.White);

            spriteBatch.Draw(texture, dstMiddle, srcMiddle, Color.White);
            spriteBatch.Draw(texture, dstMiddleLeft, srcMiddleLeft, Color.White);
            spriteBatch.Draw(texture, dstMiddleRight, srcMiddleRight, Color.White);
        }
    }
}