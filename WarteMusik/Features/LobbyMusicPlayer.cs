using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace WarteMusik.Features
{
    public sealed class LobbyMusicPlayer
    {
        private const float PumpIntervalSeconds = 0.25f;
        private const float RescanIntervalSeconds = 5f;

        private readonly Plugin _plugin;

        private SpeakerToy _speaker;
        private CoroutineHandle _loop;
        private Task<float[]> _decode;
        private string _decodingTrack;
        private float _sinceRescan;

        public LobbyMusicPlayer(Plugin plugin)
        {
            _plugin = plugin;
        }

        public bool IsRunning => _loop.IsRunning;

        public void Start()
        {
            if (IsRunning)
                return;

            _plugin.Library.Refresh();
            _sinceRescan = 0f;

            if (_plugin.Library.Count == 0)
            {
                Logger.Warn($"[WarteMusik] No .mp3 files in '{_plugin.Library.Directory}' - " +
                            $"looking again every {RescanIntervalSeconds:F0}s.");
            }

            _loop = Timing.RunCoroutine(Run());
        }

        public void Stop()
        {
            if (_loop.IsRunning)
                Timing.KillCoroutines(_loop);

            _loop = default(CoroutineHandle);

            _decode = null;
            _decodingTrack = null;

            if (_speaker == null)
                return;

            try
            {
                _speaker.Stop();
                _speaker.Destroy();
            }
            catch (Exception exception)
            {
                Logger.Warn($"[WarteMusik] Could not clean up the speaker: {exception.Message}");
            }

            _speaker = null;
        }

        private IEnumerator<float> Run()
        {
            if (_plugin.Config.StartDelaySeconds > 0f)
                yield return Timing.WaitForSeconds(_plugin.Config.StartDelaySeconds);

            CreateSpeaker();

            while (_speaker != null)
            {
                Pump();
                yield return Timing.WaitForSeconds(PumpIntervalSeconds);
            }
        }

        private void CreateSpeaker()
        {
            _speaker = SpeakerToy.Create(Vector3.zero, networkSpawn: false);
            _speaker.ControllerId = _plugin.Config.SpeakerControllerId;
            _speaker.IsSpatial = false;
            _speaker.Volume = Mathf.Max(0f, _plugin.Config.Volume);
            _speaker.MaxDistance = Mathf.Max(1f, _plugin.Config.HearingRange);
            _speaker.ValidPlayers = player => player != null
                                              && !player.IsHost
                                              && _plugin.Preferences.WantsMusic(player.ReferenceHub);

            _speaker.Stop();
            _speaker.Spawn();

            _plugin.LogDebug($"Speaker created (controller {_speaker.ControllerId}, volume {_speaker.Volume}, " +
                             $"range {_speaker.MaxDistance}).");
        }

        private void Pump()
        {
            if (_speaker == null)
                return;

            if (_plugin.Library.Count == 0 && !Rescan())
                return;

            if (_decode != null && _decode.IsCompleted)
            {
                Task<float[]> finished = _decode;
                string track = _decodingTrack;

                _decode = null;
                _decodingTrack = null;

                Enqueue(finished, track);
            }

            if (_decode == null && _speaker.QueuedClipsCount == 0)
                StartNextDecode();
        }

        private bool Rescan()
        {
            _sinceRescan += PumpIntervalSeconds;

            if (_sinceRescan < RescanIntervalSeconds)
                return false;

            _sinceRescan = 0f;
            _plugin.Library.Refresh();

            if (_plugin.Library.Count == 0)
                return false;

            Logger.Info($"[WarteMusik] {_plugin.Library.Count} track(s) found - starting playback.");
            return true;
        }

        private void Enqueue(Task<float[]> finished, string track)
        {
            string name = track == null ? "?" : Path.GetFileName(track);

            if (finished.IsFaulted)
            {
                Exception exception = finished.Exception == null
                    ? null
                    : finished.Exception.GetBaseException();

                Logger.Warn($"[WarteMusik] '{name}' could not be decoded and is skipped: " +
                            (exception == null ? "unknown error" : exception.Message));
                return;
            }

            float[] samples = finished.Result;

            if (samples == null || samples.Length == 0)
            {
                Logger.Warn($"[WarteMusik] '{name}' contains no audio data and is skipped.");
                return;
            }

            bool loop = _plugin.Library.Count == 1 && _plugin.Config.LoopPlaylist;

            _speaker.Play(samples, queue: true, loop: loop);

            _plugin.LogDebug($"'{name}' queued ({samples.Length / Mp3Decoder.TargetSampleRate}s, loop: {loop}).");
        }

        private void StartNextDecode()
        {
            if (_plugin.Library.Count == 1 && _plugin.Config.LoopPlaylist && _speaker.IsPlaying)
                return;

            string next;
            if (!_plugin.Library.TryGetNext(out next))
                return;

            int maxSeconds = _plugin.Config.MaxTrackSeconds;

            _decodingTrack = next;
            _decode = Task.Run(() => Mp3Decoder.Decode(next, maxSeconds));
        }
    }
}
