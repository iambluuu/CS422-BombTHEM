using System;

namespace Client.Animation {
    public static class Easing {
        public static float Linear(float t) => t;
        public static float QuadraticEaseIn(float t) => t * t;
        public static float QuadraticEaseOut(float t) => t * (2 - t);
        public static float QuadraticEaseInOut(float t) => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
        public static float CubicEaseIn(float t) => t * t * t;
        public static float CubicEaseOut(float t) => --t * t * t + 1;
        public static float CubicEaseInOut(float t) => (t < 0.5f) ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
        public static float ExpoEaseIn(float t) => (t == 0) ? 0 : (float)Math.Pow(2, 10 * (t - 1));
    }
}