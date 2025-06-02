using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Client.Scene {
    public class SceneNode {
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly List<SceneNode> _children = [];
        private SceneNode _parent = null;
        private Matrix _localTransform = Matrix.Identity;

        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Rotation { get; set; } = 0f;
        public Vector2 Scale { get; set; } = Vector2.One;

        public void AttachChild(SceneNode child) {
            child._parent = this;
            _lock.EnterWriteLock();
            try {
                _children.Add(child);
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public void DetachChild(SceneNode child) {
            child._parent = null;
            _lock.EnterWriteLock();
            try {
                _children.Remove(child);
            } finally {
                _lock.ExitWriteLock();
            }
        }

        public void DetachAllChildren() {
            _lock.EnterWriteLock();
            try {
                foreach (var child in _children) {
                    child._parent = null;
                }
                _children.Clear();
            } finally {
                _lock.ExitWriteLock();
            }
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
            List<SceneNode> snapshot;
            _lock.EnterReadLock();
            try {
                snapshot = _children.ToList();
            } finally {
                _lock.ExitReadLock();
            }

            foreach (var child in snapshot) {
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
            List<SceneNode> snapshot;
            _lock.EnterReadLock();
            try {
                snapshot = _children.ToList();
            } finally {
                _lock.ExitReadLock();
            }

            foreach (var child in snapshot) {
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
