using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Client.ContentHolder {
    public static class AudioHolder {
        private static readonly Dictionary<string, SoundEffect> _sounds = [];
        private static readonly Dictionary<string, Song> _musics = [];
        private static ContentManager _content;

        public static void SetContentManager(ContentManager content) {
            _content = content;
        }

        public static SoundEffect GetSound(string name) {
            if (_sounds.TryGetValue(name, out SoundEffect value)) {
                return value;
            } else {
                if (_content == null)
                    throw new System.Exception("ContentManager not set in AudioHolder");

                SoundEffect soundEffect = _content.Load<SoundEffect>("Audio/Sound/" + name);
                _sounds[name] = soundEffect;
                return soundEffect;
            }
        }

        public static Song GetMusic(string name) {
            if (_musics.TryGetValue(name, out Song value)) {
                return value;
            } else {
                if (_content == null)
                    throw new System.Exception("ContentManager not set in AudioHolder");

                Song music = _content.Load<Song>("Audio/Music/" + name);
                _musics[name] = music;
                return music;
            }
        }

        public static void UnloadAll() {
            foreach (var sound in _sounds.Values) {
                sound.Dispose();
            }

            foreach (var Music in _musics.Values) {
                Music.Dispose();
            }

            _sounds.Clear();
            _musics.Clear();
        }
    }
}
