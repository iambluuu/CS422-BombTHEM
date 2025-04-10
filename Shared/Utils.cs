namespace Shared {
    public static class Utils {
        private static readonly Random rng = new(DateTime.Now.Millisecond);

        public static int RandomInt(int max) {
            return rng.Next(max);
        }

        public static int RandomInt(int min, int max) {
            return rng.Next(min, max);
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
