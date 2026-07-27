using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly string[] SupportedExtensions = { ".wav", ".mp3" };
        private static readonly System.Random _random = new System.Random();

        private static readonly HashSet<string> _muted = new HashSet<string>();

        private static readonly Dictionary<string, int> _volumePercent = new Dictionary<string, int>();

        private static SpeakerToy[] _speakers = new SpeakerToy[0];
        private static float[] _samples;
        private static string _loadedFile;

        private static int Steps => Mathf.Clamp(Main.Instance.WarteMusikVolumeSteps, 1, 16);

        internal static void SetMuted(Player player, bool muted)
        {
            if (player?.UserId == null)
                return;

            if (muted)
                _muted.Add(player.UserId);
            else
                _muted.Remove(player.UserId);
        }

        internal static void SetVolume(Player player, float percent)
        {
            if (player?.UserId == null)
                return;

            _volumePercent[player.UserId] = Mathf.Clamp(Mathf.RoundToInt(percent), 0, 100);
        }

        private static int BucketFor(Player player)
        {
            int steps = Steps;

            if (player?.UserId == null)
                return steps;

            if (_muted.Contains(player.UserId))
                return 0;

            int percent = _volumePercent.TryGetValue(player.UserId, out int v) ? v : 100;
            return Mathf.Clamp(Mathf.RoundToInt(percent / 100f * steps), 0, steps);
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
        }

        public static void Disable()
        {
            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
            ServerEvents.RoundStarted -= OnRoundStarted;
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

                int steps = Steps;
                float master = Mathf.Clamp01(Main.Instance.WarteMusikVolume);
                bool loop = Main.Instance.WarteMusikLoop;
                byte baseId = Main.Instance.WarteMusikControllerId;

                _speakers = new SpeakerToy[steps];

                for (int i = 0; i < steps; i++)
                {
                    int bucket = i + 1;

                    SpeakerToy speaker = SpeakerToy.Create(Vector3.zero);
                    speaker.ControllerId = (byte)(baseId + i);
                    speaker.IsSpatial = false;
                    speaker.MinDistance = 0f;
                    speaker.MaxDistance = 10000f;
                    speaker.Volume = master * bucket / steps;
                    speaker.ValidPlayers = pl => BucketFor(pl) == bucket;
                    speaker.Play(samples, queue: false, loop: loop);

                    _speakers[i] = speaker;
                }

                Logger.Debug($"[WarteMusik] {steps} Lautsprecher erstellt (ControllerIds {baseId}-{baseId + steps - 1}), Wiedergabe gestartet.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[WarteMusik] Konnte Musik nicht starten: {ex}");
            }
        }

        private static void OnRoundStarted() => StopMusic();

        private static void StopMusic()
        {
            foreach (SpeakerToy speaker in _speakers)
            {
                if (speaker == null)
                    continue;

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

            _speakers = new SpeakerToy[0];
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
