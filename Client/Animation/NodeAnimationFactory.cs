using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Client.Animation {
    public static class AnimationFactory {
        // Creates a falling animation
        public static INodeAnimation CreateFallingAnimation(AnimatedNode node, float distance, float duration, bool fade = true) {
            Vector2 startPos = node.Position;
            Vector2 endPos = startPos + new Vector2(0, distance);

            var parallel = new ParallelAnimation();
            parallel.AddAnimation(new MoveAnimation(node, endPos, duration, false, Easing.CubicEaseIn));

            if (fade) {
                parallel.AddAnimation(new FadeAnimation(node, 0f, duration, false));
            }

            return parallel;
        }

        // Creates a floating (bobbing up and down) animation
        public static INodeAnimation CreateFloatingAnimation(AnimatedNode node, float amplitude, float period) {
            Vector2 basePos = node.Position;

            return new FunctionAnimation(
                node,
                basePos,
                (time) => new Vector2(0, (float)Math.Sin(time * 2 * Math.PI / period) * amplitude),
                period,
                true
            );
        }

        // Creates a spin animation
        public static INodeAnimation CreateSpinAnimation(AnimatedNode node, float rotationsPerSecond, float duration, bool isLooping = true) {
            float startRotation = node.Rotation;
            float endRotation = startRotation + MathHelper.TwoPi * rotationsPerSecond * duration;

            return new RotateAnimation(node, endRotation, duration, isLooping);
        }

        public static INodeAnimation CreateLaunchAnimation(AnimatedNode node, float yOffset, float duration) {
            var sequence = new SequenceAnimation();
            var initPos = node.Position;
            var peakPos = new Vector2(initPos.X, initPos.Y - yOffset);
            sequence.AddAnimation(new MoveAnimation(node, peakPos, duration / 2, false, Easing.QuadraticEaseOut));
            sequence.AddAnimation(new MoveAnimation(node, initPos, duration / 2, false, Easing.QuadraticEaseIn, startPosition: peakPos));

            return sequence;
        }
    }
}