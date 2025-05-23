namespace Client {
    public static class GameValues {
        public const int TILE_SIZE = 48;
        public const int SCREEN_WIDTH = 1280;
        public const int SCREEN_HEIGHT = 720;
        public const int FPS = 60;
        public const float TIME_STEP = 1f / FPS;
        public const float GRAVITY = 9.81f; // Gravity constant in m/s^2
        public const float JUMP_VELOCITY = 5f; // Initial jump velocity in m/s
    }
}