using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Component {
    public class LinearLayout : IComponent {
        public enum Orientation {
            Horizontal,
            Vertical
        }

        public enum SizeMode {
            Fixed,       // Use exact pixel dimensions (current behavior)
            MatchParent, // Take full size of parent (like match_parent)
            WrapContent  // Size based on content (like wrap_content)
        }

        // Layout properties
        public required Orientation LayoutOrientation { get; set; }
        public int Spacing { get; set; } = 0;
        private Padding _padding = new(0);
        public List<IComponent> Components { get; set; } = [];
        public List<int> Weights { get; set; } = [];
        private int _totalWeight = 0;

        // Size mode properties
        public SizeMode WidthMode { get; set; } = SizeMode.Fixed;
        public SizeMode HeightMode { get; set; } = SizeMode.Fixed;

        // Parent reference for MatchParent sizing
        private IComponent _parent;

#nullable enable
        private IComponent? _focusedComponent = null;
#nullable disable

        // Store desired size separately from actual size
        private Vector2 _desiredSize;

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
                _desiredSize = value;
                UpdateActualSize();
            }
        }

        public float Padding { get => _padding.Left; set { _padding = new Padding(value); RearrangeComponents(); } }
        public float PaddingLeft { get => _padding.Left; set { _padding.Left = value; RearrangeComponents(); } }
        public float PaddingRight { get => _padding.Right; set { _padding.Right = value; RearrangeComponents(); } }
        public float PaddingTop { get => _padding.Top; set { _padding.Top = value; RearrangeComponents(); } }
        public float PaddingBottom { get => _padding.Bottom; set { _padding.Bottom = value; RearrangeComponents(); } }

        // Set the parent component for MatchParent sizing
        public void SetParent(IComponent parent) {
            _parent = parent;
            UpdateActualSize();
        }

        // Calculate and update the actual size based on sizing modes
        private void UpdateActualSize() {
            var newSize = _desiredSize;

            // Handle width sizing
            if (WidthMode == SizeMode.MatchParent && _parent != null) {
                newSize.X = _parent.Size.X;
            } else if (WidthMode == SizeMode.WrapContent) {
                newSize.X = MeasureContentWidth();
            }

            // Handle height sizing
            if (HeightMode == SizeMode.MatchParent && _parent != null) {
                newSize.Y = _parent.Size.Y;
            } else if (HeightMode == SizeMode.WrapContent) {
                newSize.Y = MeasureContentHeight();
            }

            // Update the actual size
            base.Size = newSize;
            RearrangeComponents();
        }

        // Measure content width for WrapContent mode
        private float MeasureContentWidth() {
            if (Components.Count == 0) return PaddingLeft + PaddingRight;

            if (LayoutOrientation == Orientation.Horizontal) {
                // For horizontal layout, sum all component widths plus spacing
                float totalWidth = PaddingLeft + PaddingRight;

                foreach (var component in Components.Where(c => c.IsVisible)) {
                    totalWidth += component.Size.X;
                }

                // Add spacing between components
                if (Components.Count(c => c.IsVisible) > 1) {
                    totalWidth += Spacing * (Components.Count(c => c.IsVisible) - 1);
                }

                return totalWidth;
            } else {
                // For vertical layout, find the widest component
                float maxWidth = 0;
                foreach (var component in Components.Where(c => c.IsVisible)) {
                    maxWidth = Math.Max(maxWidth, component.Size.X);
                }
                return maxWidth + PaddingLeft + PaddingRight;
            }
        }

        // Measure content height for WrapContent mode
        private float MeasureContentHeight() {
            if (Components.Count == 0) return PaddingTop + PaddingBottom;

            if (LayoutOrientation == Orientation.Vertical) {
                // For vertical layout, sum all component heights plus spacing
                float totalHeight = PaddingTop + PaddingBottom;

                foreach (var component in Components.Where(c => c.IsVisible)) {
                    totalHeight += component.Size.Y;
                }

                // Add spacing between components
                if (Components.Count(c => c.IsVisible) > 1) {
                    totalHeight += Spacing * (Components.Count(c => c.IsVisible) - 1);
                }

                return totalHeight;
            } else {
                // For horizontal layout, find the tallest component
                float maxHeight = 0;
                foreach (var component in Components.Where(c => c.IsVisible)) {
                    maxHeight = Math.Max(maxHeight, component.Size.Y);
                }
                return maxHeight + PaddingTop + PaddingBottom;
            }
        }

        private void RearrangeComponents() {
            var currentPosition = Position + new Vector2(PaddingLeft, PaddingTop);
            float availableWidth = Size.X - PaddingLeft - PaddingRight;
            float availableHeight = Size.Y - PaddingTop - PaddingBottom;
            int visibleComponentCount = Components.Count(c => c.IsVisible);
            float totalSpacing = visibleComponentCount > 1 ? Spacing * (visibleComponentCount - 1) : 0;

            // First pass: Calculate sizes for weighted components
            for (int i = 0; i < Components.Count; i++) {
                if (!Components[i].IsVisible) continue;

                var component = Components[i];
                int weight = Weights[i];

                if (LayoutOrientation == Orientation.Horizontal) {
                    float compWidth = weight > 0
                        ? (availableWidth - totalSpacing) * weight / _totalWeight
                        : component.Size.X;
                    component.Size = new Vector2(compWidth, availableHeight);
                } else {
                    float compHeight = weight > 0
                        ? (availableHeight - totalSpacing) * weight / _totalWeight
                        : component.Size.Y;
                    component.Size = new Vector2(availableWidth, compHeight);
                }
            }

            // Second pass: Position components
            for (int i = 0; i < Components.Count; i++) {
                if (!Components[i].IsVisible) continue;

                var component = Components[i];
                component.Position = currentPosition;

                if (LayoutOrientation == Orientation.Horizontal) {
                    currentPosition.X += component.Size.X + Spacing;
                } else {
                    currentPosition.Y += component.Size.Y + Spacing;
                }
            }

            // Recalculate size if using WrapContent
            if (WidthMode == SizeMode.WrapContent || HeightMode == SizeMode.WrapContent) {
                var newSize = base.Size;
                if (WidthMode == SizeMode.WrapContent) {
                    newSize.X = MeasureContentWidth();
                }
                if (HeightMode == SizeMode.WrapContent) {
                    newSize.Y = MeasureContentHeight();
                }
                // Update size without triggering infinite recursion
                base.Size = newSize;
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

        // Child components can have their own size modes
        public void AddComponent(IComponent component, SizeMode widthMode, SizeMode heightMode, int weight = 1, int position = -1) {
            // If component is another LinearLayout, set its size modes
            if (component is LinearLayout childLayout) {
                childLayout.WidthMode = widthMode;
                childLayout.HeightMode = heightMode;
                childLayout.SetParent(this);
            }

            AddComponent(component, weight, position);
        }

        public void RemoveComponent(IComponent component) {
            int index = Components.IndexOf(component);
            if (index != -1) {
                _totalWeight -= Weights[index];
                Components.RemoveAt(index);
                Weights.RemoveAt(index);
                RearrangeComponents();
            }
        }

        public void ClearComponents() {
            Components.Clear();
            Weights.Clear();
            _totalWeight = 0;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            // Check if parent size changed when using MatchParent
            if ((WidthMode == SizeMode.MatchParent || HeightMode == SizeMode.MatchParent) && _parent != null) {
                UpdateActualSize();
            }

            foreach (var component in Components) {
                component.Update(gameTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            // Draw components from back to front
            for (int i = 0; i < Components.Count; i++) {
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
                // Handle mouse clicks from front to back
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

            // Forward other events to components
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
                _focusedComponent.OnUnfocus();
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

        public override bool HitTest(Point point) {
            // First check if the point is within the layout's bounds
            Rectangle bounds = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            if (!bounds.Contains(point)) return false;

            // Then check if the point hits any child component
            for (int i = Components.Count - 1; i >= 0; i--) {
                if (Components[i].IsVisible && Components[i].HitTest(point)) {
                    return true;
                }
            }

            // If the point is within the layout but not on any child component,
            // it's still considered a hit on the layout itself
            return true;
        }
    }
}