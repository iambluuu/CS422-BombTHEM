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
        public required Orientation LayoutOrientation { get; set; }
        public int Spacing { get; set; } = 0;
        public List<IComponent> Components { get; set; } = [];
        public Gravity Gravity { get; set; } = Gravity.TopLeft;

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
        protected override float MeasureContentWidth() {
            if (Components.Count == 0) return PaddingLeft + PaddingRight;

            if (LayoutOrientation == Orientation.Horizontal) {
                float totalWidth = PaddingLeft + PaddingRight;

                foreach (var component in Components.Where(c => c.IsVisible)) {
                    totalWidth += component.RequestedWidth;
                }
                if (Components.Count(c => c.IsVisible) > 1) {
                    totalWidth += Spacing * (Components.Count(c => c.IsVisible) - 1);
                }

                return totalWidth;
            } else {
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
                float totalHeight = PaddingTop + PaddingBottom;

                foreach (var component in Components.Where(c => c.IsVisible)) {
                    totalHeight += component.RequestedHeight;
                }
                if (Components.Count(c => c.IsVisible) > 1) {
                    totalHeight += Spacing * (Components.Count(c => c.IsVisible) - 1);
                }

                return totalHeight;
            } else {
                float maxHeight = 0;
                foreach (var component in Components.Where(c => c.IsVisible)) {
                    maxHeight = Math.Max(maxHeight, component.RequestedHeight);
                }
                return maxHeight + PaddingTop + PaddingBottom;
            }
        }

        public void ArrangeComponents() {
            MeasureAndSetSize();
            float availableWidth = Width - PaddingLeft - PaddingRight;
            float availableHeight = Height - PaddingTop - PaddingBottom;
            int visibleComponentCount = Components.Count(c => c.IsVisible);
            float totalSpacing = visibleComponentCount > 1 ? Spacing * (visibleComponentCount - 1) : 0;
            int totalWeight = 0;
            float totalFixedSize = 0;
            float remainingSpace = LayoutOrientation == Orientation.Horizontal ? availableWidth : availableHeight;
            foreach (var component in Components.Where(c => c.IsVisible)) {
                if (component.Weight > 0) {
                    totalWeight += component.Weight;
                } else {
                    if (LayoutOrientation == Orientation.Horizontal) {
                        totalFixedSize += component.RequestedWidth;
                    } else {
                        totalFixedSize += component.RequestedHeight;
                    }
                }
            }
            remainingSpace -= (totalFixedSize + totalSpacing);
            Vector2 contentStartPosition = CalculateContentStartPosition(
                availableWidth,
                availableHeight,
                LayoutOrientation == Orientation.Horizontal ? totalFixedSize + totalSpacing : 0,
                LayoutOrientation == Orientation.Vertical ? totalFixedSize + totalSpacing : 0
            );
            var currentPosition = contentStartPosition;
            foreach (var component in Components.Where(c => c.IsVisible)) {
                float componentWidth, componentHeight;

                if (LayoutOrientation == Orientation.Horizontal) {
                    if (component.Weight > 0) {
                        componentWidth = remainingSpace * component.Weight / totalWeight;
                    } else {
                        if (component.WidthMode == SizeMode.MatchParent) {
                            componentWidth = availableWidth;
                        } else {
                            componentWidth = component.RequestedWidth;
                        }
                    }
                    if (component.HeightMode == SizeMode.MatchParent) {
                        componentHeight = availableHeight;
                    } else {
                        componentHeight = component.RequestedHeight;
                    }
                    float componentY = currentPosition.Y;
                    Gravity effectiveGravity = component.LayoutGravity != Gravity.NoGravity ? component.LayoutGravity : Gravity;

                    if ((effectiveGravity & Gravity.Bottom) != 0) {
                        componentY = currentPosition.Y + availableHeight - componentHeight;
                    } else if ((effectiveGravity & Gravity.CenterVertical) != 0) {
                        componentY = currentPosition.Y + (availableHeight - componentHeight) / 2;
                    }
                    component.SetMeasuredSize(componentWidth, componentHeight);
                    component.Position = new Vector2(currentPosition.X, componentY);
                    currentPosition.X += componentWidth + Spacing;
                } else {
                    if (component.Weight > 0) {
                        componentHeight = remainingSpace * component.Weight / totalWeight;
                    } else {
                        if (component.HeightMode == SizeMode.MatchParent) {
                            componentHeight = availableHeight;
                        } else {
                            componentHeight = component.RequestedHeight;
                        }
                    }
                    if (component.WidthMode == SizeMode.MatchParent) {
                        componentWidth = availableWidth;
                    } else {
                        componentWidth = component.RequestedWidth;
                    }
                    float componentX = currentPosition.X;
                    Gravity effectiveGravity = component.LayoutGravity != Gravity.NoGravity ? component.LayoutGravity : Gravity;

                    if ((effectiveGravity & Gravity.Right) != 0) {
                        componentX = currentPosition.X + availableWidth - componentWidth;
                    } else if ((effectiveGravity & Gravity.CenterHorizontal) != 0) {
                        componentX = currentPosition.X + (availableWidth - componentWidth) / 2;
                    }
                    component.SetMeasuredSize(componentWidth, componentHeight);
                    component.Position = new Vector2(componentX, currentPosition.Y);
                    currentPosition.Y += componentHeight + Spacing;
                }
            }
        }

        private Vector2 CalculateContentStartPosition(float availableWidth, float availableHeight,
                                                     float contentWidth, float contentHeight) {
            Vector2 startPosition = Position + new Vector2(PaddingLeft, PaddingTop);
            if (LayoutOrientation == Orientation.Horizontal) {
                if (contentWidth < availableWidth) {
                    if ((Gravity & Gravity.CenterHorizontal) != 0) {
                        startPosition.X += (availableWidth - contentWidth) / 2;
                    } else if ((Gravity & Gravity.Right) != 0) {
                        startPosition.X += availableWidth - contentWidth;
                    }
                }
            } else {
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
            if (WidthMode == SizeMode.WrapContent) {
                SetMeasuredSize(MeasureContentWidth(), Height);
            }

            if (HeightMode == SizeMode.WrapContent) {
                SetMeasuredSize(Width, MeasureContentHeight());
            }
        }

        public void AddComponent(IComponent component, int position = -1) {
            if (component.Weight > 0) {
                if (LayoutOrientation == Orientation.Horizontal) {
                    if (component.WidthMode == SizeMode.Fixed) {
                        component.WidthMode = SizeMode.MatchParent;
                    }
                } else {
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
            ArrangeComponents();

            foreach (var component in Components) {
                component.Update(gameTime);
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (!IsVisible) return;
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
            Rectangle bounds = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            if (!bounds.Contains(point)) return false;
            for (int i = Components.Count - 1; i >= 0; i--) {
                if (Components[i].IsVisible && Components[i].HitTest(point)) {
                    return true;
                }
            }
            return true;
        }
    }
}
