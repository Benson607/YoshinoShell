using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace YoshinoShell
{
    internal class SoundLoader
    {
        private Dictionary<string, SoundPlayer> sound_players = new Dictionary<string, SoundPlayer>();

        private SoundPlayer? CurrentPlayer;
        private string? CurrentMusicName;

        public void Load(string name, string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (!sound_players.ContainsKey(name))
            {
                var player = new SoundPlayer(path);
                player.Load();
                sound_players[name] = player;
            }
        }

        public void Play(string name)
        {
            if (sound_players.TryGetValue(name, out var player))
            {
                Task.Run(() => player.Play());
            }
        }

        public void PlayMusic(string name, bool loop)
        {
            if (sound_players.TryGetValue(name, out var player))
            {
                Stop();

                CurrentMusicName = name;

                if (loop)
                {
                    player.PlayLooping();
                }
                else
                {
                    player.Play();
                }
            }
        }

        public void Stop()
        {
            CurrentPlayer?.Stop();
            CurrentMusicName = null;
        }

        public void Release()
        {
            if (CurrentPlayer != null)
            {
                CurrentPlayer.Stop();
                CurrentPlayer = null;
            }

            CurrentMusicName = null;

            foreach (var player in sound_players.Values)
            {
                player.Stop();
                player.Dispose();
            }
            sound_players.Clear();
        }
    }
}
