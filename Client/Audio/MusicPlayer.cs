using Microsoft.Xna.Framework.Media;

using Client.ContentHolder;

namespace Client.Audio {
    public static class MusicPlayer {
        private static string _currentSong;

        public static void Initialize() {
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.75f;
        }

        public static void Play(string name) {
            if (_currentSong != null && _currentSong == name) {
                return;
            }

            _currentSong = name;
            MediaPlayer.Play(AudioHolder.GetMusic(name));
        }

        public static void Stop() {
            MediaPlayer.Stop();
            _currentSong = null;
        }

        public static void Pause() {
            MediaPlayer.Pause();
        }

        public static void Resume() {
            MediaPlayer.Resume();
        }
    }
}
