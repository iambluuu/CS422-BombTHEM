using System;

namespace Client.Scene {
    public class BitmaskReferences {
        private static readonly int[,] grid = new int[,]{
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0},
            {0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0},
            {0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 0, 0},
            {0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0, 0, 0},
            {0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 0, 0, 0},
            {0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 1, 1, 0, 0, 0, 0},
            {0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0},
            {0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 0},
            {0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0}
        };

        private static bool initialized = false;
        private static readonly (int, int)?[] position = new (int, int)?[512];

        static BitmaskReferences() {
            Initialize();
        }

        private static void Initialize() {
            if (initialized) {
                return;
            }

            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);

            for (int i = 0; i < rows; i += 3) {
                for (int j = 0; j < cols; j += 3) {
                    if (i + 2 >= rows || j + 2 >= cols)
                        continue;

                    if (grid[i + 1, j + 1] != 1)
                        continue;

                    int bitmask = 0;
                    for (int k = 0; k < 3; k++) {
                        for (int l = 0; l < 3; l++) {
                            bitmask |= (grid[i + k, j + l] << (k * 3 + l));
                        }
                    }

                    if (position[bitmask].HasValue) {
                        var existing = position[bitmask].Value;
                        Console.WriteLine($"Duplicate bitmask {bitmask} at {i / 3}, {j / 3} and {existing.Item1}, {existing.Item2}");
                    }

                    position[bitmask] = (i / 3, j / 3);
                }
            }

            initialized = true;
        }

        public static (int, int) GetPosition(bool[,] localArea) {
            if (localArea == null || localArea.GetLength(0) != 3 || localArea.GetLength(1) != 3) {
                throw new ArgumentException("localArea must be a 3x3 boolean array");
            }

            if (localArea[1, 1] == false) {
                throw new ArgumentException("The center of the local area must be true");
            }

            (int, int)[] directions = {
                (1, 0), (-1, 0), (0, 1), (0, -1),
                (0, 0), (1, 0), (0, 1), (1, 1),
            };

            int bitmask = 1 << 4;
            for (int i = 0; i < 8; i++) {
                int dx = directions[i].Item1, dy = directions[i].Item2;
                int x = 1 + dx, y = 1 + dy;

                if (i < 4) {
                    if (localArea[x, y]) {
                        bitmask |= 1 << ((dx + 1) * 3 + (dy + 1));
                    }
                } else {
                    if (localArea[x, y] && localArea[x - 1, y] && localArea[x, y - 1] && localArea[x - 1, y - 1]) {
                        bitmask |= 1 << ((2 * dx) * 3 + (2 * dy));
                    }
                }
            }

            return GetPosition(bitmask);
        }

        private static (int, int) GetPosition(int bitmask) {
            if (!initialized)
                Initialize();

            if (bitmask < 0 || bitmask >= 512 || !position[bitmask].HasValue) {
                throw new ArgumentException($"Bitmask {bitmask} not found in references");
            }

            return position[bitmask].Value;
        }
    }
}