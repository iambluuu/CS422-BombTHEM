using Client.ContentHolder;

namespace Client.Audio {
    public static class SoundPlayer {
        private static float _volume;

        public static void Initialize() {
            _volume = 0.5f;
        }

        public static void Play(string name, float volume = 1f) {
            AudioHolder.GetSound(name).Play(_volume * volume, 0f, 0f);
        }
    }
}