using System.Runtime.InteropServices;


namespace Shared {
    public static class Utils {
        [DllImport("User32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        public static float GetDPI(IntPtr windowHandle) {
            return GetDpiForWindow(windowHandle);
        }

        public static float GetDPI() {
            return GetDPI(IntPtr.Zero);
        }

        public static float ToPixels(float points) {
            return points * GetDPI() / 72f;
        }

        public static float ToPixels(float points, IntPtr windowHandle) {
            return points * 72f / GetDPI(windowHandle);
        }

        private static readonly Random rng = new(DateTime.Now.Millisecond);

        public static int RandomInt(int max) {
            return rng.Next(max);
        }

        public static int RandomInt(int min, int max) {
            return rng.Next(min, max);
        }

        public static T randomList<T>(List<T> list) {
            if (list.Count == 0) {
                throw new ArgumentException("List is empty");
            }

            return list[RandomInt(list.Count)];
        }

        public static List<T> Shuffle<T>(List<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }

            return list;
        }
    }
}
