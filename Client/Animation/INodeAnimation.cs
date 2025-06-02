using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Client.Scene;

namespace Client.Animation {
    // Base interface for all animations
    public interface INodeAnimation {
        bool IsComplete { get; }
        void Update(GameTime gameTime);
        void Reset();
    }

    // Base class for all transform animations
    public abstract class NodeTransformAnimation : INodeAnimation {
        protected float _duration;
        protected float _elapsedTime;
        protected bool _isLooping;
        protected Func<float, float> _easingFunction;

        public bool IsComplete => !_isLooping && _elapsedTime >= _duration;

        protected NodeTransformAnimation(float duration, bool isLooping = false, Func<float, float> easingFunction = null) {
            _duration = duration;
            _isLooping = isLooping;
            _elapsedTime = 0f;
            _easingFunction = easingFunction ?? Easing.Linear;
        }

        public virtual void Update(GameTime gameTime) {
            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_elapsedTime > _duration && _isLooping) {
                _elapsedTime %= _duration;
            }
        }

        public virtual void Reset() {
            _elapsedTime = 0f;
        }

        // Get normalized progress (0 to 1) with easing applied
        protected float GetProgress() {
            float normalizedTime = Math.Min(_elapsedTime / _duration, 1f);
            return _easingFunction(normalizedTime);
        }

    }

    // Moves a node from start to end position
    public class MoveAnimation : NodeTransformAnimation {
        private readonly Vector2 _startPosition;
        private readonly Vector2 _endPosition;
        private readonly AnimatedNode _targetNode;

        public MoveAnimation(AnimatedNode targetNode, Vector2 endPosition, float duration, bool isLooping = false, Func<float, float> easingFunction = null, Vector2? startPosition = null)
            : base(duration, isLooping, easingFunction) {
            _targetNode = targetNode;
            _startPosition = startPosition ?? targetNode.Position;
            _endPosition = endPosition;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            float progress = GetProgress();
            _targetNode.Position = Vector2.Lerp(_startPosition, _endPosition, progress);
        }

        public override void Reset() {
            base.Reset();
            _targetNode.Position = _startPosition;
        }
    }

    // Rotates a node
    public class RotateAnimation : NodeTransformAnimation {
        private readonly float _startRotation;
        private readonly float _endRotation;
        private readonly AnimatedNode _targetNode;

        public RotateAnimation(AnimatedNode targetNode, float endRotation, float duration, bool isLooping = false, Func<float, float> easingFunction = null)
            : base(duration, isLooping, easingFunction) {
            _targetNode = targetNode;
            _startRotation = targetNode.Rotation;
            _endRotation = endRotation;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            float progress = GetProgress();
            _targetNode.Rotation = MathHelper.Lerp(_startRotation, _endRotation, progress);
        }

        public override void Reset() {
            base.Reset();
            _targetNode.Rotation = _startRotation;
        }
    }

    // Scales a node
    public class ScaleAnimation : NodeTransformAnimation {
        private readonly Vector2 _startScale;
        private readonly Vector2 _endScale;
        private readonly AnimatedNode _targetNode;

        public ScaleAnimation(AnimatedNode targetNode, Vector2 endScale, float duration, bool isLooping = false, Func<float, float> easingFunction = null)
            : base(duration, isLooping, easingFunction) {
            _targetNode = targetNode;
            _startScale = targetNode.Scale;
            _endScale = endScale;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            float progress = GetProgress();
            _targetNode.Scale = Vector2.Lerp(_startScale, _endScale, progress);
        }

        public override void Reset() {
            base.Reset();
            _targetNode.Scale = _startScale;
        }
    }

    // Fades a node in/out by modifying its alpha
    public class FadeAnimation : NodeTransformAnimation {
        private readonly float _startAlpha;
        private readonly float _endAlpha;
        private readonly AnimatedNode _targetNode;

        public FadeAnimation(AnimatedNode targetNode, float endAlpha, float duration, bool isLooping = false, Func<float, float> easingFunction = null)
            : base(duration, isLooping, easingFunction) {
            _targetNode = targetNode;
            _startAlpha = targetNode.Alpha;
            _endAlpha = endAlpha;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            float progress = GetProgress();
            _targetNode.Alpha = MathHelper.Lerp(_startAlpha, _endAlpha, progress);
        }

        public override void Reset() {
            base.Reset();
            _targetNode.Alpha = _startAlpha;
        }
    }

    // Color transition animation
    public class ColorAnimation : NodeTransformAnimation {
        private readonly Color _startColor;
        private readonly Color _endColor;
        private readonly AnimatedNode _targetNode;

        public ColorAnimation(AnimatedNode targetNode, Color endColor, float duration, bool isLooping = false, Func<float, float> easingFunction = null)
            : base(duration, isLooping, easingFunction) {
            _targetNode = targetNode;
            _startColor = targetNode.Color;
            _endColor = endColor;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            float progress = GetProgress();
            _targetNode.Color = Color.Lerp(_startColor, _endColor, progress);
        }

        public override void Reset() {
            base.Reset();
            _targetNode.Color = _startColor;
        }
    }

    // Complex motion path animation
    public class PathAnimation : NodeTransformAnimation {
        private readonly Vector2[] _pathPoints;
        private readonly AnimatedNode _targetNode;

        public PathAnimation(AnimatedNode targetNode, Vector2[] pathPoints, float duration, bool isLooping = false, Func<float, float> easingFunction = null)
            : base(duration, isLooping, easingFunction) {
            _targetNode = targetNode;
            _pathPoints = pathPoints;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            float progress = GetProgress();

            // Find the segment of the path we're currently on
            float segmentLength = 1.0f / (_pathPoints.Length - 1);
            int index = Math.Min((int)(progress / segmentLength), _pathPoints.Length - 2);
            float segmentProgress = (progress - index * segmentLength) / segmentLength;

            // Interpolate between the two points of this segment
            _targetNode.Position = Vector2.Lerp(_pathPoints[index], _pathPoints[index + 1], segmentProgress);
        }

        public override void Reset() {
            base.Reset();
            if (_pathPoints.Length > 0) {
                _targetNode.Position = _pathPoints[0];
            }
        }
    }

    // Animation that follows a mathematical function (for effects like sine wave, bounce, etc.)
    public class FunctionAnimation : NodeTransformAnimation {
        private readonly Vector2 _basePosition;
        private readonly Func<float, Vector2> _motionFunction;
        private readonly AnimatedNode _targetNode;

        public FunctionAnimation(AnimatedNode targetNode, Vector2 basePosition, Func<float, Vector2> motionFunction,
                                float duration, bool isLooping = true, Func<float, float> easingFunction = null)
            : base(duration, isLooping, easingFunction) {
            _targetNode = targetNode;
            _basePosition = basePosition;
            _motionFunction = motionFunction;
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            float progress = _elapsedTime / _duration; // Raw progress for time-based functions
            Vector2 offset = _motionFunction(progress);
            _targetNode.Position = _basePosition + offset;
        }

        public override void Reset() {
            base.Reset();
            _targetNode.Position = _basePosition;
        }
    }

    // Frame animation (similar to your existing VFXNode)
    public class FrameAnimation : INodeAnimation {
        private readonly Texture2D _texture;
        private readonly int _frameCount;
        private readonly float _frameTime;
        private readonly float _duration;
        private readonly bool _isLooping;
        private readonly AnimatedNode _targetNode;

        private int _currentFrame;
        private float _elapsedFrameTime;
        private float _elapsedTime;

        public bool IsComplete => !_isLooping && (_elapsedTime >= _duration || _currentFrame >= _frameCount);

        public FrameAnimation(AnimatedNode targetNode, Texture2D texture, int frameCount,
                             float frameTime = 0.1f, float duration = float.MaxValue, bool isLooping = true) {
            _targetNode = targetNode;
            _texture = texture;
            _frameCount = frameCount;
            _frameTime = frameTime;
            _duration = duration;
            _isLooping = isLooping;
            _currentFrame = 0;
            _elapsedFrameTime = 0f;
            _elapsedTime = 0f;

            // Set the texture and frame info on the target node
            _targetNode.SetFrameInfo(_texture, _frameCount);
        }

        public void Update(GameTime gameTime) {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _elapsedFrameTime += deltaTime;
            _elapsedTime += deltaTime;

            if (_elapsedFrameTime >= _frameTime) {
                _elapsedFrameTime = 0f;
                _currentFrame++;

                if (_currentFrame >= _frameCount) {
                    if (_isLooping) {
                        _currentFrame = 0;
                    } else {
                        _currentFrame = _frameCount - 1;
                    }
                }

                _targetNode.CurrentFrame = _currentFrame;
            }

            if (_elapsedTime >= _duration && !IsComplete) {
                // Handle duration expiration
                if (!_isLooping) {
                    _currentFrame = _frameCount - 1;
                    _targetNode.CurrentFrame = _currentFrame;
                }
            }
        }

        public void Reset() {
            _currentFrame = 0;
            _elapsedFrameTime = 0f;
            _elapsedTime = 0f;
            _targetNode.CurrentFrame = _currentFrame;
        }
    }

    // Sequence animation executes animations one after another
    public class SequenceAnimation : INodeAnimation {
        private readonly List<INodeAnimation> _animations;
        private int _currentAnimationIndex;

        public bool IsComplete => _currentAnimationIndex >= _animations.Count;

        public SequenceAnimation(params INodeAnimation[] animations) {
            _animations = new(animations);
            _currentAnimationIndex = 0;
        }

        public void Update(GameTime gameTime) {
            if (IsComplete) return;

            INodeAnimation currentAnimation = _animations[_currentAnimationIndex];
            currentAnimation.Update(gameTime);

            if (currentAnimation.IsComplete) {
                _currentAnimationIndex++;
            }
        }

        public void Reset() {
            foreach (var animation in _animations) {
                animation.Reset();
            }
            _currentAnimationIndex = 0;
        }

        public void AddAnimation(INodeAnimation animation) {
            _animations.Add(animation);
        }
    }

    // Parallel animation executes multiple animations simultaneously 
    public class ParallelAnimation : INodeAnimation {
        private readonly List<INodeAnimation> _animations;

        public bool IsComplete => _animations.All(a => a.IsComplete);

        public ParallelAnimation(params INodeAnimation[] animations) {
            _animations = new List<INodeAnimation>(animations);
        }

        public void Update(GameTime gameTime) {
            foreach (var animation in _animations) {
                if (!animation.IsComplete) {
                    animation.Update(gameTime);
                }
            }
        }

        public void Reset() {
            foreach (var animation in _animations) {
                animation.Reset();
            }
        }

        public void AddAnimation(INodeAnimation animation) {
            _animations.Add(animation);
        }
    }

    // The main AnimatedNode class extending your SpriteNode
    public class AnimatedNode : SpriteNode {
        private readonly List<INodeAnimation> _animations = new List<INodeAnimation>();
        private readonly List<INodeAnimation> _animationsToRemove = new List<INodeAnimation>();
        private readonly List<INodeAnimation> _animationsToAdd = new List<INodeAnimation>();
        public float Alpha { get; set; } = 1.0f;
        public Color Color { get; set; } = Color.White;
        public int CurrentFrame { get; set; }

        // Frame info for sprite sheet animations
        private Texture2D _frameTexture;
        private int _frameCount = 1;

        public AnimatedNode(Texture2D texture, Vector2 size) : base(texture, size) {
            _frameTexture = texture;
            Position = Vector2.Zero; // Or whatever your SpriteNode uses as default
            Rotation = 0f;
        }

        public void SetFrameInfo(Texture2D texture, int frameCount) {
            _frameTexture = texture;
            _frameCount = Math.Max(1, frameCount);
        }

        public void AddAnimation(INodeAnimation animation) {
            _animationsToAdd.Add(animation);
        }

        public void RemoveAnimation(INodeAnimation animation) {
            _animationsToRemove.Add(animation);
        }

        public void ClearAnimations() {
            _animations.Clear();
            _animationsToAdd.Clear();
            _animationsToRemove.Clear();
        }

        protected override void UpdateCurrent(GameTime gameTime) {
            base.UpdateCurrent(gameTime);

            // Remove completed animations
            foreach (var animation in _animationsToRemove) {
                _animations.Remove(animation);
            }
            _animationsToRemove.Clear();

            // Add new animations
            foreach (var animation in _animationsToAdd) {
                _animations.Add(animation);
            }
            _animationsToAdd.Clear();

            // Update active animations
            foreach (var animation in _animations) {
                animation.Update(gameTime);
                if (animation.IsComplete) {
                    _animationsToRemove.Add(animation);
                }
            }

            if (_animations.Count <= 0) {
                DetachSelf();
            }
        }

        protected override void DrawCurrent(SpriteBatch spriteBatch, Matrix transform) {
            if (_frameCount <= 0) return;

            // // Apply the node's local transformation
            // Matrix localTransform = Matrix.CreateTranslation(new Vector3(-_origin, 0)) *
            //                   Matrix.CreateScale(new Vector3(Scale, 1)) *
            //                   Matrix.CreateRotationZ(Rotation) *
            //                   Matrix.CreateTranslation(new Vector3(Position, 0));
            // Matrix worldTransform = localTransform * transform;

            Matrix worldTransform = transform;

            Vector2 position = Vector2.Transform(Vector2.Zero, worldTransform);
            float rotation = RotationFromMatrix(worldTransform);
            Vector2 scale = ScaleFromMatrix(worldTransform);

            int frameWidth = _frameTexture.Width / _frameCount;
            int frameHeight = _frameTexture.Height;
            Vector2 textureScale = new Vector2(Math.Min(_size.X / frameWidth, _size.Y / frameHeight));

            Rectangle sourceRect = new Rectangle(
                CurrentFrame * frameWidth,
                0,
                frameWidth,
                frameHeight
            );

            // Apply color with alpha
            Color drawColor = Color;
            drawColor.A = (byte)(Alpha * 255);

            spriteBatch.Draw(
                _frameTexture,
                position,
                sourceRect,
                drawColor,
                rotation,
                _origin,
                scale * textureScale,
                SpriteEffects.None,
                0f
            );
        }
    }
}