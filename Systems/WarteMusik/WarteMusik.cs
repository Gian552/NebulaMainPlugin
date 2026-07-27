using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using NLayer;
using LabApi.Features.Audio;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Paths;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace NebMainPluginLabApi.Systems.WarteMusik
{
    public static class WarteMusik
    {
        private const int MaxSpeakers = 32;

        private static readonly string[] SupportedExtensions = { ".wav", ".mp3" };
        private static readonly System.Random _random = new System.Random();

        private static readonly HashSet<string> _muted = new HashSet<string>();

        private static readonly Dictionary<string, int> _volumePercent = new Dictionary<string, int>();

        private static readonly Dictionary<string, PlayerStream> _playerStreams = new Dictionary<string, PlayerStream>();

        private static SpeakerToy[] _speakers = new SpeakerToy[0];
        private static float[] _samples;
        private static float[] _activeSamples;
        private static string _loadedFile;
        private static bool _playing;
        private static bool _limitWarned;

        private sealed class PlayerStream
        {
            internal SpeakerToy Speaker;
            internal int Slot;
        }

        private static VolumeMode Mode => Main.Instance.WarteMusikVolumeMode;

        private static byte BaseId => Main.Instance.WarteMusikControllerId;

        private static int MaxSlots => Mathf.Clamp(256 - BaseId, 1, MaxSpeakers);

        private static int Steps => Mathf.Clamp(Main.Instance.WarteMusikVolumeSteps, 1, MaxSlots);

        private static int MaxStreams => Mathf.Clamp(Main.Instance.WarteMusikMaxStreams, 1, MaxSlots);

        internal static void SetMuted(Player player, bool muted)
        {
            if (player?.UserId == null)
                return;

            if (muted)
                _muted.Add(player.UserId);
            else
                _muted.Remove(player.UserId);

            EnsurePlayerStream(player);
        }

        internal static void SetVolume(Player player, float percent)
        {
            if (player?.UserId == null)
                return;

            _volumePercent[player.UserId] = Mathf.Clamp(Mathf.RoundToInt(percent), 0, 100);

            EnsurePlayerStream(player);
        }

        private static int PercentFor(Player player)
        {
            if (player?.UserId == null)
                return 100;

            if (_muted.Contains(player.UserId))
                return 0;

            return _volumePercent.TryGetValue(player.UserId, out int v) ? v : 100;
        }

        private static int BucketFor(Player player)
        {
            int steps = Steps;

            if (player?.UserId == null)
                return steps;

            return Mathf.Clamp(Mathf.RoundToInt(PercentFor(player) / 100f * steps), 0, steps);
        }

        public static void Enable()
        {
            try
            {
                string folder = GetMusicFolder();
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                int trackCount = GetTracks(folder).Length;
                Logger.Info($"[WarteMusik] Music-Ordner: {folder} ({trackCount} Track(s))");
            }
            catch (Exception ex)
            {
                Logger.Error($"[WarteMusik] Konnte Music-Ordner nicht anlegen: {ex.Message}");
            }

            ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
            ServerEvents.RoundStarted += OnRoundStarted;
            PlayerEvents.Joined += OnJoined;
            PlayerEvents.Left += OnLeft;
        }

        public static void Disable()
        {
            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
            ServerEvents.RoundStarted -= OnRoundStarted;
            PlayerEvents.Joined -= OnJoined;
            PlayerEvents.Left -= OnLeft;
            StopMusic();
        }

        private static void OnWaitingForPlayers()
        {
            if (!Main.Instance.WarteMusikEnabled)
                return;

            try
            {
                var samples = LoadSamples();
                if (samples == null)
                    return;

                StopMusic();

                _activeSamples = samples;
                _playing = true;
                _limitWarned = false;

                if (Mode == VolumeMode.Spieler)
                    StartPlayerMode();
                else
                    StartSegmentMode();
            }
            catch (Exception ex)
            {
                Logger.Error($"[WarteMusik] Konnte Musik nicht starten: {ex}");
            }
        }

        private static void StartSegmentMode()
        {
            int steps = Steps;
            float master = Mathf.Clamp01(Main.Instance.WarteMusikVolume);
            bool loop = Main.Instance.WarteMusikLoop;
            byte baseId = BaseId;

            _speakers = new SpeakerToy[steps];

            for (int i = 0; i < steps; i++)
            {
                int bucket = i + 1;

                SpeakerToy speaker = CreateSpeaker((byte)(baseId + i), master * bucket / steps);
                speaker.ValidPlayers = pl => BucketFor(pl) == bucket;
                speaker.Play(_activeSamples, queue: false, loop: loop);

                _speakers[i] = speaker;
            }

            Logger.Debug($"[WarteMusik] Modus Segmente: {steps} Lautsprecher erstellt (ControllerIds {baseId}-{baseId + steps - 1}), Wiedergabe gestartet.");
        }

        private static void StartPlayerMode()
        {
            foreach (Player player in Player.List)
                EnsurePlayerStream(player);

            Logger.Debug($"[WarteMusik] Modus Spieler: {_playerStreams.Count} Stream(s) fuer {Player.List.Count()} Spieler gestartet (max. {MaxStreams}).");
        }

        private static void EnsurePlayerStream(Player player)
        {
            if (!_playing || Mode != VolumeMode.Spieler || _activeSamples == null)
                return;

            if (player?.UserId == null)
                return;

            float volume = Mathf.Clamp01(Main.Instance.WarteMusikVolume) * (PercentFor(player) / 100f);

            if (_playerStreams.TryGetValue(player.UserId, out PlayerStream stream))
            {
                if (volume <= 0f)
                    RemovePlayerStream(player.UserId);
                else if (stream.Speaker != null)
                    stream.Speaker.Volume = volume;

                return;
            }

            if (volume <= 0f)
                return;

            int slot = NextFreeSlot();
            if (slot < 0)
            {
                if (!_limitWarned)
                {
                    _limitWarned = true;
                    Logger.Warn($"[WarteMusik] Alle {MaxStreams} Audio-Streams belegt - weitere Spieler hoeren keine Musik. Erhoehe WarteMusikMaxStreams oder nutze den Modus Segmente.");
                }

                return;
            }

            try
            {
                int position = ReferencePosition();
                string userId = player.UserId;

                SpeakerToy speaker = CreateSpeaker((byte)(BaseId + slot), volume);
                speaker.ValidPlayers = pl => pl != null && pl.UserId == userId;
                speaker.Play(_activeSamples, queue: false, loop: Main.Instance.WarteMusikLoop);

                if (speaker.Transmitter != null)
                    speaker.Transmitter.CurrentPosition = position;

                _playerStreams[userId] = new PlayerStream { Speaker = speaker, Slot = slot };
            }
            catch (Exception ex)
            {
                Logger.Error($"[WarteMusik] Konnte Stream fuer {player.Nickname} nicht starten: {ex.Message}");
            }
        }

        private static SpeakerToy CreateSpeaker(byte controllerId, float volume)
        {
            SpeakerToy speaker = SpeakerToy.Create(Vector3.zero);
            speaker.ControllerId = controllerId;
            speaker.IsSpatial = false;
            speaker.MinDistance = 0f;
            speaker.MaxDistance = 10000f;
            speaker.Volume = volume;
            return speaker;
        }

        private static int NextFreeSlot()
        {
            int max = MaxStreams;

            for (int slot = 0; slot < max; slot++)
            {
                if (!_playerStreams.Values.Any(s => s.Slot == slot))
                    return slot;
            }

            return -1;
        }

        private static int ReferencePosition()
        {
            foreach (PlayerStream stream in _playerStreams.Values)
            {
                AudioTransmitter transmitter = stream.Speaker?.Transmitter;
                if (transmitter != null && transmitter.IsPlaying)
                    return transmitter.CurrentPosition;
            }

            return 0;
        }

        private static void RemovePlayerStream(string userId)
        {
            if (userId == null || !_playerStreams.TryGetValue(userId, out PlayerStream stream))
                return;

            _playerStreams.Remove(userId);
            DestroySpeaker(stream.Speaker);
        }

        private static void OnJoined(PlayerJoinedEventArgs ev) => EnsurePlayerStream(ev.Player);

        private static void OnLeft(PlayerLeftEventArgs ev) => RemovePlayerStream(ev.Player?.UserId);

        private static void OnRoundStarted() => StopMusic();

        private static void StopMusic()
        {
            foreach (SpeakerToy speaker in _speakers)
                DestroySpeaker(speaker);

            _speakers = new SpeakerToy[0];

            foreach (PlayerStream stream in _playerStreams.Values.ToArray())
                DestroySpeaker(stream.Speaker);

            _playerStreams.Clear();

            _activeSamples = null;
            _playing = false;
        }

        private static void DestroySpeaker(SpeakerToy speaker)
        {
            if (speaker == null)
                return;

            try
            {
                speaker.Transmitter?.Stop();
                speaker.Destroy();
            }
            catch (Exception ex)
            {
                Logger.Debug($"[WarteMusik] Fehler beim Stoppen: {ex.Message}");
            }
        }

        private static string GetMusicFolder()
        {
            string folder = Main.Instance.WarteMusikFolder;
            if (string.IsNullOrWhiteSpace(folder))
                folder = "Music";

            return Path.IsPathRooted(folder) ? folder : Path.Combine(PathManager.Configs.FullName, folder);
        }

        private static string[] GetTracks(string folder)
            => Directory.GetFiles(folder)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

        private static float[] LoadSamples()
        {
            string folder = GetMusicFolder();
            if (!Directory.Exists(folder))
            {
                Logger.Error($"[WarteMusik] Music-Ordner nicht gefunden: {folder}");
                return null;
            }

            string[] tracks = GetTracks(folder);
            if (tracks.Length == 0)
            {
                string[] others = Directory.GetFiles(folder);
                if (others.Length > 0)
                    Logger.Warn($"[WarteMusik] {folder} enthaelt keine unterstuetzten Dateien (.wav/.mp3) - andere Formate wie ogg mit ffmpeg konvertieren.");
                else
                    Logger.Warn($"[WarteMusik] Keine Tracks in {folder} - Musik bleibt aus.");
                return null;
            }

            string path = tracks[_random.Next(tracks.Length)];

            if (_samples != null && _loadedFile == path)
                return _samples;

            _samples = Path.GetExtension(path).ToLowerInvariant() == ".mp3"
                ? ReadMp3ToMono48k(path)
                : ReadWavToMono48k(path);
            _loadedFile = path;
            Logger.Info($"[WarteMusik] Spiele {Path.GetFileName(path)} ({_samples.Length / (float)AudioTransmitter.SampleRate:F1}s Audio).");
            return _samples;
        }

        #region Audio Loader

        private static float[] ReadMp3ToMono48k(string path)
        {
            using (var mpeg = new MpegFile(path))
            {
                int channels = mpeg.Channels;
                if (channels <= 0)
                    throw new InvalidDataException("MP3 ohne Kanaele?");

                var mono = new List<float>(1 << 20);
                float[] buf = new float[4096 * channels];
                float sum = 0f;
                int chIdx = 0;
                int read;

                while ((read = mpeg.ReadSamples(buf, 0, buf.Length)) > 0)
                {
                    for (int i = 0; i < read; i++)
                    {
                        sum += buf[i];
                        if (++chIdx == channels)
                        {
                            mono.Add(sum / channels);
                            sum = 0f;
                            chIdx = 0;
                        }
                    }
                }

                float[] data = mono.ToArray();
                return mpeg.SampleRate == AudioTransmitter.SampleRate
                    ? data
                    : Resample(data, mpeg.SampleRate, AudioTransmitter.SampleRate);
            }
        }

        private static float[] ReadWavToMono48k(string path)
        {
            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                if (new string(br.ReadChars(4)) != "RIFF")
                    throw new InvalidDataException("Keine WAV-Datei (RIFF-Header fehlt)");
                br.ReadInt32();
                if (new string(br.ReadChars(4)) != "WAVE")
                    throw new InvalidDataException("Keine WAV-Datei (WAVE-Kennung fehlt)");

                short format = 0, channels = 0, bits = 0;
                int sampleRate = 0;
                float[] data = null;

                while (fs.Position + 8 <= fs.Length)
                {
                    string chunkId = new string(br.ReadChars(4));
                    int chunkSize = br.ReadInt32();
                    long next = fs.Position + chunkSize + (chunkSize & 1);

                    if (chunkId == "fmt ")
                    {
                        format = br.ReadInt16();
                        channels = br.ReadInt16();
                        sampleRate = br.ReadInt32();
                        br.ReadInt32();
                        br.ReadInt16();
                        bits = br.ReadInt16();

                        if (format == unchecked((short)0xFFFE))
                        {
                            br.ReadInt16();
                            br.ReadInt16();
                            br.ReadInt32();
                            format = br.ReadInt16();
                        }
                    }
                    else if (chunkId == "data")
                    {
                        if (channels == 0)
                            throw new InvalidDataException("fmt-Chunk fehlt vor dem data-Chunk");

                        data = ConvertToMono(br.ReadBytes(chunkSize), format, channels, bits);
                    }

                    fs.Position = next;
                }

                if (data == null)
                    throw new InvalidDataException("data-Chunk fehlt");

                return sampleRate == AudioTransmitter.SampleRate
                    ? data
                    : Resample(data, sampleRate, AudioTransmitter.SampleRate);
            }
        }

        private static float[] ConvertToMono(byte[] raw, short format, short channels, short bits)
        {
            int bytesPerSample = bits / 8;
            int frameCount = raw.Length / (bytesPerSample * channels);
            float[] mono = new float[frameCount];

            for (int frame = 0; frame < frameCount; frame++)
            {
                float sum = 0f;
                for (int ch = 0; ch < channels; ch++)
                {
                    int o = (frame * channels + ch) * bytesPerSample;
                    float sample;

                    if (format == 3 && bits == 32)
                        sample = BitConverter.ToSingle(raw, o);
                    else if (format == 1 && bits == 16)
                        sample = BitConverter.ToInt16(raw, o) / 32768f;
                    else if (format == 1 && bits == 24)
                        sample = ((raw[o] << 8) | (raw[o + 1] << 16) | (raw[o + 2] << 24)) / 2147483648f;
                    else if (format == 1 && bits == 32)
                        sample = BitConverter.ToInt32(raw, o) / 2147483648f;
                    else if (format == 1 && bits == 8)
                        sample = (raw[o] - 128) / 128f;
                    else
                        throw new NotSupportedException($"WAV-Format nicht unterstuetzt (Format {format}, {bits} bit)");

                    sum += sample;
                }

                mono[frame] = sum / channels;
            }

            return mono;
        }

        private static float[] Resample(float[] input, int fromRate, int toRate)
        {
            int outLen = (int)((long)input.Length * toRate / fromRate);
            float[] output = new float[outLen];
            float step = (float)fromRate / toRate;

            for (int i = 0; i < outLen; i++)
            {
                float pos = i * step;
                int i0 = (int)pos;
                int i1 = Math.Min(i0 + 1, input.Length - 1);
                float frac = pos - i0;
                output[i] = input[i0] + (input[i1] - input[i0]) * frac;
            }

            return output;
        }

        #endregion
    }
}
