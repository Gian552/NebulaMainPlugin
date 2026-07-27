using System;
using System.Collections.Generic;
using System.IO;
using LabApi.Loader;
using Logger = LabApi.Features.Console.Logger;

namespace WarteMusik.Features
{
    /// <summary>
    /// Keeps track of the .mp3 files in the music folder and hands them out one
    /// after another (shuffled or alphabetically, depending on the config).
    /// </summary>
    public sealed class MusicLibrary
    {
        private readonly Plugin _plugin;
        private readonly Random _random = new Random();
        private readonly Queue<string> _upcoming = new Queue<string>();

        private string[] _tracks = new string[0];
        private bool _exhausted;

        public MusicLibrary(Plugin plugin)
        {
            _plugin = plugin;

            // .../LabAPI/configs/<port>/WarteMusik/music/
            Directory = Path.Combine(plugin.GetConfigDirectory().FullName, plugin.Config.MusicFolder);
        }

        /// <summary>Absolute path of the folder the tracks are read from.</summary>
        public string Directory { get; }

        public int Count => _tracks.Length;

        /// <summary>
        /// Creates the music folder if needed and re-reads its contents. Called
        /// whenever a new lobby starts, so tracks can be added without a restart.
        /// </summary>
        public void Refresh()
        {
            _upcoming.Clear();
            _exhausted = false;

            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                _tracks = System.IO.Directory.GetFiles(Directory, "*.mp3", SearchOption.TopDirectoryOnly);
                Array.Sort(_tracks, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception exception)
            {
                _tracks = new string[0];
                Logger.Error($"[WarteMusik] Could not read the music folder '{Directory}': {exception.Message}");
                return;
            }

            _plugin.LogDebug($"{_tracks.Length} track(s) found in '{Directory}'.");
        }

        /// <summary>
        /// Returns the next track to play. Yields <c>false</c> once the playlist
        /// ran out and looping is disabled, or if there are no tracks at all.
        /// </summary>
        public bool TryGetNext(out string path)
        {
            path = null;

            if (_tracks.Length == 0)
                return false;

            if (_upcoming.Count == 0)
            {
                if (_exhausted && !_plugin.Config.LoopPlaylist)
                    return false;

                Fill();
            }

            if (_upcoming.Count == 0)
                return false;

            path = _upcoming.Dequeue();

            if (_upcoming.Count == 0)
                _exhausted = true;

            return true;
        }

        /// <summary>Refills the play queue, shuffled if the config asks for it.</summary>
        private void Fill()
        {
            string[] order = (string[])_tracks.Clone();

            if (_plugin.Config.Shuffle)
            {
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = _random.Next(i + 1);
                    string swap = order[i];
                    order[i] = order[j];
                    order[j] = swap;
                }
            }

            foreach (string track in order)
                _upcoming.Enqueue(track);
        }
    }
}
