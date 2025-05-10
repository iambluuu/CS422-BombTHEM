using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Client.Component {
    public enum Orientation {
        Horizontal,
        Vertical
    }

    public class LinearLayout : IComponent {
        // Layout properties
        public required Orientation LayoutOrientation { get; set; }
        public int Spacing { get; set; } = 0;
        public List<IComponent> Components { get; set; } = [];
        public Gravity Gravity { get; set; } = Gravity.TopLeft; // Layout's gravity for all children

#nullable enable
        private IComponent? _focusedComponent = null;
#nullable disable

        public override Vector2 Position {
            get => base.Position;
            set {
                base.Position = value;
                ArrangeComponents();
            }
        }

        // Override MeasureContent methods to calculate wrap_content size
        protected override float MeasureContentWidth() {
            if (Components.Count == 0) return PaddingLeft + PaddingRight;

            if (LayoutOrientation == Orientation.Horizontal) {
                // For horizontal layout, sum all component widths plus spacing
                float totalWidth = PaddingLeft + PaddingRight;

                foreach (var component in Components.Where(c => c.IsVisible)) {
                    totalWidth += component.RequestedWidth;
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
                    maxWidth = Math.Max(maxWidth, component.RequestedWidth);
                }
                return maxWidth + PaddingLeft + PaddingRight;
            }
        }

        protected override float MeasureContentHeight() {
            if (Components.Count == 0) return PaddingTop + PaddingBottom;

            if (LayoutOrientation == Orientation.Vertical) {
                // For vertical layout, sum all component heights plus spacing
                float totalHeight = PaddingTop + PaddingBottom;

                foreach (var component in Components.Where(c => c.IsVisible)) {
                    totalHeight += component.RequestedHeight;
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
                    maxHeight = Math.Max(maxHeight, component.RequestedHeight);
                }
                return maxHeight + PaddingTop + PaddingBottom;
            }
        }

        public void ArrangeComponents() {
            // First measure our own size based on size mode
            MeasureAndSetSize();

            // Calculate available space
            float availableWidth = Width - PaddingLeft - PaddingRight;
            float availableHeight = Height - PaddingTop - PaddingBottom;
            int visibleComponentCount = Components.Count(c => c.IsVisible);
            float totalSpacing = visibleComponentCount > 1 ? Spacing * (visibleComponentCount - 1) : 0;

            // Calculate total weight and determine fixed sizes
            int totalWeight = 0;
            float totalFixedSize = 0;
            float remainingSpace = LayoutOrientation == Orientation.Horizontal ? availableWidth : availableHeight;

            // First pass: Calculate sizes for fixed components and total weight
            foreach (var component in Components.Where(c => c.IsVisible)) {
                if (component.Weight > 0) {
                    totalWeight += component.Weight;
                } else {
                    // Fixed size components
                    if (LayoutOrientation == Orientation.Horizontal) {
                        totalFixedSize += component.RequestedWidth;
                    } else {
                        totalFixedSize += component.RequestedHeight;
                    }
                }
            }

            // Subtract fixed sizes and spacing from available space
            remainingSpace -= (totalFixedSize + totalSpacing);

            // Calculate starting position for content based on gravity
            Vector2 contentStartPosition = CalculateContentStartPosition(
                availableWidth,
                availableHeight,
                LayoutOrientation == Orientation.Horizontal ? totalFixedSize + totalSpacing : 0,
                LayoutOrientation == Orientation.Vertical ? totalFixedSize + totalSpacing : 0
            );

            // Second pass: Assign sizes and positions to components
            var currentPosition = contentStartPosition;
            foreach (var component in Components.Where(c => c.IsVisible)) {
                float componentWidth, componentHeight;

                if (LayoutOrientation == Orientation.Horizontal) {
                    // Set width based on weight or fixed size
                    if (component.Weight > 0) {
                        componentWidth = remainingSpace * component.Weight / totalWeight;
                    } else {
                        if (component.WidthMode == SizeMode.MatchParent) {
                            // For matchParent with no weight, use all remaining space
                            componentWidth = availableWidth;
                        } else {
                            // For fixed or wrap_content, use the requested size
                            componentWidth = component.RequestedWidth;
                        }
                    }

                    // Set height based on component's height mode
                    if (component.HeightMode == SizeMode.MatchParent) {
                        componentHeight = availableHeight;
                    } else {
                        componentHeight = component.RequestedHeight;
                    }

                    // Apply vertical alignment based on component's LayoutGravity if set, otherwise use parent's Gravity
                    float componentY = currentPosition.Y;
                    Gravity effectiveGravity = component.LayoutGravity != Gravity.NoGravity ? component.LayoutGravity : Gravity;

                    if ((effectiveGravity & Gravity.Bottom) != 0) {
                        componentY = currentPosition.Y + availableHeight - componentHeight;
                    } else if ((effectiveGravity & Gravity.CenterVertical) != 0) {
                        componentY = currentPosition.Y + (availableHeight - componentHeight) / 2;
                    }

                    // Update component's measured size
                    component.SetMeasuredSize(componentWidth, componentHeight);

                    // Position the component with gravity applied
                    component.Position = new Vector2(currentPosition.X, componentY);

                    // Move to next position horizontally
                    currentPosition.X += componentWidth + Spacing;
                } else { // Vertical orientation
                    // Set height based on weight or fixed size
                    if (component.Weight > 0) {
                        componentHeight = remainingSpace * component.Weight / totalWeight;
                    } else {
                        if (component.HeightMode == SizeMode.MatchParent) {
                            // For matchParent with no weight, use all available height
                            componentHeight = availableHeight;
                        } else {
                            // For fixed or wrap_content, use the requested size
                            componentHeight = component.RequestedHeight;
                        }
                    }

                    // Set width based on component's width mode
                    if (component.WidthMode == SizeMode.MatchParent) {
                        componentWidth = availableWidth;
                    } else {
                        componentWidth = component.RequestedWidth;
                    }

                    // Apply horizontal alignment based on component's LayoutGravity if set, otherwise use parent's Gravity
                    float componentX = currentPosition.X;
                    Gravity effectiveGravity = component.LayoutGravity != Gravity.NoGravity ? component.LayoutGravity : Gravity;

                    if ((effectiveGravity & Gravity.Right) != 0) {
                        componentX = currentPosition.X + availableWidth - componentWidth;
                    } else if ((effectiveGravity & Gravity.CenterHorizontal) != 0) {
                        componentX = currentPosition.X + (availableWidth - componentWidth) / 2;
                    }

                    // Update component's measured size
                    component.SetMeasuredSize(componentWidth, componentHeight);

                    // Position the component with gravity applied
                    component.Position = new Vector2(componentX, currentPosition.Y);

                    // Move to next position vertically
                    currentPosition.Y += componentHeight + Spacing;
                }
            }
        }

        private Vector2 CalculateContentStartPosition(float availableWidth, float availableHeight,
                                                     float contentWidth, float contentHeight) {
            Vector2 startPosition = Position + new Vector2(PaddingLeft, PaddingTop);

            // If layout orientation is horizontal, apply horizontal gravity to the entire content block
            if (LayoutOrientation == Orientation.Horizontal) {
                // Skip horizontal gravity calculation if total width matches parent or exceeds it
                if (contentWidth < availableWidth) {
                    if ((Gravity & Gravity.CenterHorizontal) != 0) {
                        startPosition.X += (availableWidth - contentWidth) / 2;
                    } else if ((Gravity & Gravity.Right) != 0) {
                        startPosition.X += availableWidth - contentWidth;
                    }
                }
            }
            // If layout orientation is vertical, apply vertical gravity to the entire content block
            else {
                // Skip vertical gravity calculation if total height matches parent or exceeds it
                if (contentHeight < availableHeight) {
                    if ((Gravity & Gravity.CenterVertical) != 0) {
                        startPosition.Y += (availableHeight - contentHeight) / 2;
                    } else if ((Gravity & Gravity.Bottom) != 0) {
                        startPosition.Y += availableHeight - contentHeight;
                    }
                }
            }

            return startPosition;
        }

        private void MeasureAndSetSize() {
            // Handle size modes
            if (WidthMode == SizeMode.WrapContent) {
                SetMeasuredSize(MeasureContentWidth(), Height);
            }

            if (HeightMode == SizeMode.WrapContent) {
                SetMeasuredSize(Width, MeasureContentHeight());
            }
        }

        public void AddComponent(IComponent component, int position = -1) {
            // Validate component weight/size for layout orientation
            if (component.Weight > 0) {
                if (LayoutOrientation == Orientation.Horizontal) {
                    // For weighted components in horizontal layout, width should be handled by layout
                    if (component.WidthMode == SizeMode.Fixed) {
                        component.WidthMode = SizeMode.MatchParent;
                    }
                } else {
                    // For weighted components in vertical layout, height should be handled by layout
                    if (component.HeightMode == SizeMode.Fixed) {
                        component.HeightMode = SizeMode.MatchParent;
                    }
                }
            }

            if (position < 0 || position >= Components.Count) {
                Components.Add(component);
            } else {
                Components.Insert(position, component);
            }

            ArrangeComponents();
        }

        public void RemoveComponent(IComponent component) {
            if (Components.Remove(component)) {
                ArrangeComponents();
            }
        }

        public void ClearComponents() {
            Components.Clear();
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            // Re-arrange if needed
            ArrangeComponents();

            foreach (var component in Components) {
                component.Update(gameTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;

            // Draw components from back to front
            for (int i = 0; i < Components.Count; i++) {
                if (Components[i].IsVisible) {
                    // Console.WriteLine($"Drawing component {i}: {Components[i].GetType().Name} at {Components[i].Position} with size {Components[i].Size}");
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