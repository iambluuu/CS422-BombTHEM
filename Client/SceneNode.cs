using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace Client {
    public class SceneNode {
        private readonly List<SceneNode> _children = [];
        private SceneNode _parent = null;
        private Matrix _localTransform = Matrix.Identity;

        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Rotation { get; set; } = 0f;
        public Vector2 Scale { get; set; } = Vector2.One;

        public void AttachChild(SceneNode child) {
            child._parent = this;
            _children.Add(child);
        }

        public void DetachChild(SceneNode child) {
            child._parent = null;
            _children.Remove(child);
        }

        public void DetachSelf() {
            if (_parent != null) {
                _parent.DetachChild(this);
                _parent = null;
            }
        }

        public virtual void UpdateTree(GameTime gameTime) {
            UpdateCurrent(gameTime);
            UpdateChildren(gameTime);
        }

        protected virtual void UpdateCurrent(GameTime gameTime) { }

        private void UpdateChildren(GameTime gameTime) {
            foreach (var child in _children.ToList()) {
                child.UpdateTree(gameTime);
            }
        }

        public void DrawTree(SpriteBatch spriteBatch, Matrix parentTransform) {
            Matrix transform = GetTransform() * parentTransform;

            DrawCurrent(spriteBatch, transform);
            DrawChildren(spriteBatch, transform);
        }

        protected virtual void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) { }

        private void DrawChildren(SpriteBatch spriteBatch, Matrix transform) {
            foreach (var child in _children.ToList()) {
                child.DrawTree(spriteBatch, transform);
            }
        }

        public Matrix GetTransform() {
            return
                Matrix.CreateScale(new Vector3(Scale, 1f)) *
                Matrix.CreateRotationZ(Rotation) *
                Matrix.CreateTranslation(new Vector3(Position, 0f));
        }

        protected float RotationFromMatrix(Matrix matrix) {
            return (float)System.Math.Atan2(matrix.M21, matrix.M11);
        }

        protected Vector2 ScaleFromMatrix(Matrix matrix) {
            return new Vector2(
                new Vector2(matrix.M11, matrix.M21).Length(),
                new Vector2(matrix.M12, matrix.M22).Length()
            );
        }
    }
}
